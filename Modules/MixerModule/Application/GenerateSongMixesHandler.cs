using System.Net.Http.Headers;
using System.Security.Cryptography;
using Marten;
using Microsoft.Extensions.Logging;
using WBand.Modules.FileModule.Contracts;
using WBand.Modules.MixerModule.Contracts;
using WBand.Modules.MixerModule.Domain;
using WBand.Modules.StemModule.Contracts;
using Wolverine;

namespace WBand.Modules.MixerModule.Application;

public sealed class GenerateSongMixesHandler(IHttpClientFactory httpClientFactory,
    ILogger<GenerateSongMixesHandler> logger, IDocumentStore store)
{
    public async Task Handle(GenerateSongMixes command, IDocumentSession session,
        IMessageBus bus, CancellationToken cancellationToken)
    {
        using var generationLease = await MixGenerationLock.AcquireAsync(cancellationToken);
        var batch = await session.LoadAsync<MixBatch>(command.BatchId, cancellationToken);
        if (batch is null || batch.Status == MixBatchStatus.Ready) return;
        var ownerLeaseId = batch.LeaseId;
        var startedAt = DateTimeOffset.UtcNow;
        batch.Status = MixBatchStatus.Processing;
        batch.StartedAt ??= startedAt;
        batch.HeartbeatAt = startedAt;
        batch.AttemptCount++;
        batch.Error = null;
        batch.NextAttemptAt = null;
        batch.CurrentStage = "Starting";
        batch.CurrentPlan = null;
        batch.LeaseExpiresAt = startedAt.AddMinutes(30);
        session.Store(batch); await session.SaveChangesAsync(cancellationToken);
        var workDirectory = Path.Combine(Path.GetTempPath(), "wband-mixer", batch.Id.ToString("N"));
        try
        {
            Directory.CreateDirectory(workDirectory);
            var sources = await bus.InvokeAsync<ReadyStemForMix[]>(new GetReadyStemsForMix(command.SongId), cancellationToken);
            if (sources.Length == 0) throw new InvalidOperationException("No ready stems were found.");
            var downloads = (await bus.InvokeAsync<FileDownloadAccess[]>(new GetReadyFileDownloads(
                sources.Select(source => source.FileId).ToArray()), cancellationToken)).ToDictionary(file => file.FileId);
            if (sources.Any(source => !downloads.ContainsKey(source.FileId)))
                throw new InvalidOperationException("Some stems are not ready for mixing.");
            var existingArtifacts = await session.Query<MixArtifact>()
                .Where(artifact => artifact.BatchId == batch.Id).ToListAsync(cancellationToken);
            var completedOutputs = existingArtifacts.Select(ArtifactKey).ToHashSet();
            var tracks = sources.Select(source => new AudioTrack(source.StemId,
                Path.Combine(workDirectory, $"{source.FileId:N}{ExtensionFor(downloads[source.FileId].MimeType)}"),
                source.Name, LocalizeGroup(source.Kind), source.Kind)).ToArray();
            var plans = MixPlanFactory.Create(tracks);
            if (plans.Count == 0) throw new InvalidOperationException("No instrument stems were found.");
            batch.TotalOutputCount = plans.Count;
            batch.CompletedOutputCount = MixProgress.CountCompleted(plans, completedOutputs);
            batch.CurrentStage = "Downloading sources";
            session.Store(batch);
            await session.SaveChangesAsync(cancellationToken);
            var client = httpClientFactory.CreateClient("mixer");
            var downloader = new MixSourceDownloader(client);
            foreach (var source in sources.Where(_ => batch.CompletedOutputCount < batch.TotalOutputCount))
            {
                var access = downloads[source.FileId];
                batch.CurrentPlan = source.Name;
                batch.HeartbeatAt = DateTimeOffset.UtcNow;
                session.Store(batch);
                await session.SaveChangesAsync(cancellationToken);
                await downloader.DownloadAsync(access, tracks.First(track => track.StemId == source.StemId).Path,
                    cancellationToken);
            }
            batch.CurrentStage = "Mixing";
            session.Store(batch);
            await session.SaveChangesAsync(cancellationToken);
            var workGroups = plans.GroupBy(plan => plan.Target?.StemId).ToArray();
            foreach (var workGroup in workGroups)
            {
                var pendingPlans = workGroup.Where(plan =>
                    !completedOutputs.Contains(ArtifactKey(ToArtifactKind(plan.Kind),
                        plan.Target?.StemId, "wav")) ||
                    !completedOutputs.Contains(ArtifactKey(ToArtifactKind(plan.Kind),
                        plan.Target?.StemId, "mp3"))).ToArray();
                if (pendingPlans.Length == 0) continue;

                batch.HeartbeatAt = DateTimeOffset.UtcNow;
                batch.LeaseExpiresAt = batch.HeartbeatAt.Value.AddMinutes(30);
                batch.CurrentPlan = string.Join(" + ", pendingPlans.Select(plan => $"{plan.Target?.Name} ({plan.Kind})"));
                session.Store(batch);
                await session.SaveChangesAsync(cancellationToken);
                var renderOutputs = pendingPlans.Select(plan => new MixOutput(plan,
                    Path.Combine(workDirectory, $"{plan.Name}.wav"),
                    Path.Combine(workDirectory, $"{plan.Name}.mp3"))).ToArray();
                using (var rendering = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    rendering.CancelAfter(TimeSpan.FromMinutes(10));
                    try { await FfmpegMixRunner.RunAsync(renderOutputs, rendering.Token); }
                    catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
                    {
                        throw new TimeoutException("FFmpeg render exceeded ten minutes.", exception);
                    }
                }
                foreach (var renderOutput in renderOutputs)
                {
                    var plan = renderOutput.Plan;
                    var artifactKind = ToArtifactKind(plan.Kind);
                    var outputs = new[]
                    {
                        (Path: renderOutput.WavePath, Mime: "audio/wav", Format: "wav"),
                        (Path: renderOutput.Mp3Path, Mime: "audio/mpeg", Format: "mp3"),
                    };
                    foreach (var output in outputs.Where(output => !completedOutputs.Contains(
                        ArtifactKey(artifactKind, plan.Target?.StemId, output.Format))))
                    {
                        await using var input = File.OpenRead(output.Path);
                        var fileId = Guid.CreateVersion7();
                        var checksum = Convert.ToHexString(await SHA256.HashDataAsync(input, cancellationToken)).ToLowerInvariant();
                        var key = $"bands/{command.BandId:D}/songs/{command.SongId:D}/mixes/" +
                            $"{fileId:D}/{plan.Name}.{output.Format}";
                        var upload = await bus.InvokeAsync<CreateFileUploadResult>(new CreateFileUpload(
                            fileId, command.BandId, key, $"{plan.Name}.{output.Format}", output.Mime,
                            input.Length, checksum, command.RequestedBy), cancellationToken);
                        if (upload.Ticket is not { } ticket)
                            throw new InvalidOperationException($"Mix upload rejected: {upload.Error}");
                        input.Position = 0;
                        using var content = new StreamContent(input);
                        content.Headers.ContentType = new MediaTypeHeaderValue(output.Mime);
                        content.Headers.ContentLength = input.Length;
                        using var response = await client.PutAsync(ticket.UploadUrl, content,
                            cancellationToken);
                        response.EnsureSuccessStatusCode();
                        using var verification = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        verification.CancelAfter(TimeSpan.FromMinutes(2));
                        var completed = await bus.InvokeAsync<CompleteFileUploadResult>(
                            new CompleteFileUpload(fileId), verification.Token);
                        if (completed.File is null)
                        {
                            if (completed.Error == "file_object_unavailable")
                                throw new IOException("Mix verification temporarily unavailable: file_object_unavailable");
                            throw new InvalidOperationException($"Mix verification failed: {completed.Error}");
                        }
                        session.Store(new MixArtifact { Id = MixProgress.ArtifactId(batch.Id, artifactKind,
                            plan.Target?.StemId, output.Format), BatchId = batch.Id,
                            SongId = command.SongId, BandId = command.BandId, FileId = fileId,
                            Kind = artifactKind, TargetStemId = plan.Target?.StemId,
                            Group = plan.Target?.Group ?? "Общий микс",
                            Name = plan.Target is null ? "Полный микс" : plan.Target.Name,
                            Format = output.Format, CreatedAt = DateTimeOffset.UtcNow });
                        completedOutputs.Add(ArtifactKey(artifactKind, plan.Target?.StemId,
                            output.Format));
                        batch.CompletedOutputCount = MixProgress.CountCompleted(plans, completedOutputs);
                        batch.RetryCount = 0;
                        batch.HeartbeatAt = DateTimeOffset.UtcNow;
                        batch.LeaseExpiresAt = batch.HeartbeatAt.Value.AddMinutes(30);
                        session.Store(batch);
                        await session.SaveChangesAsync(cancellationToken);
                    }
                }
            }
            batch.Status = MixBatchStatus.Ready; batch.CompletedAt = DateTimeOffset.UtcNow;
            batch.RetryCount = 0;
            batch.HeartbeatAt = batch.CompletedAt;
            batch.CurrentStage = "Ready";
            batch.CurrentPlan = null;
            batch.LeaseId = null;
            batch.LeaseExpiresAt = null;
            session.Store(batch); await session.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            batch.Status = MixBatchStatus.Queued;
            batch.HeartbeatAt = DateTimeOffset.UtcNow;
            batch.CurrentStage = "Queued";
            batch.LeaseId = null;
            batch.LeaseExpiresAt = null;
            try { await SaveRecoveryAsync(batch, ownerLeaseId); }
            catch (Exception exception) { logger.LogWarning(exception,
                "Could not return cancelled mix batch {BatchId} to the queue", batch.Id); }
            throw;
        }
        catch (Exception exception)
        {
            var retry = MixRetryPolicy.IsTransient(exception)
                && ++batch.RetryCount <= 5;
            if (retry) logger.LogWarning(exception, "Mix batch {BatchId} will be retried", batch.Id);
            else logger.LogError(exception, "Mix batch {BatchId} failed", batch.Id);
            batch.Status = retry ? MixBatchStatus.Queued : MixBatchStatus.Failed;
            batch.Error = exception.GetBaseException().Message;
            batch.NextAttemptAt = retry ? DateTimeOffset.UtcNow.AddSeconds(Math.Min(60, 5 * batch.AttemptCount)) : null;
            batch.CurrentStage = retry ? "Retry scheduled" : "Failed";
            batch.LeaseId = null;
            batch.LeaseExpiresAt = null;
            batch.CompletedAt = retry ? null : DateTimeOffset.UtcNow;
            await SaveRecoveryAsync(batch, ownerLeaseId);
        }
        finally
        {
            try { if (batch.Status is MixBatchStatus.Ready or MixBatchStatus.Failed
                && Directory.Exists(workDirectory)) Directory.Delete(workDirectory, true); }
            catch (IOException exception) { logger.LogWarning(exception,
                "Could not remove mixer working directory {Directory}", workDirectory); }
        }
    }

    private async Task SaveRecoveryAsync(MixBatch failed, Guid? ownerLeaseId)
    {
        // A failed transaction cannot safely be reused, and its commit may have succeeded.
        // Reload committed state before scheduling recovery to avoid losing artifacts or versions.
        await using var recovery = store.LightweightSession();
        var persisted = await recovery.LoadAsync<MixBatch>(failed.Id, CancellationToken.None);
        if (persisted is null || persisted.Status == MixBatchStatus.Ready
            || persisted.LeaseId != ownerLeaseId) return;
        var artifacts = await recovery.Query<MixArtifact>().Where(artifact => artifact.BatchId == failed.Id)
            .ToListAsync(CancellationToken.None);
        persisted.CompletedOutputCount = artifacts.Where(artifact => artifact.Kind != MixArtifactKind.Full)
            .GroupBy(artifact => (artifact.Kind, artifact.TargetStemId))
            .Count(group => group.Any(artifact => artifact.Format == "wav")
                && group.Any(artifact => artifact.Format == "mp3"));
        persisted.Status = failed.Status;
        persisted.Error = failed.Error;
        persisted.NextAttemptAt = failed.NextAttemptAt;
        persisted.RetryCount = failed.RetryCount;
        persisted.CurrentStage = failed.CurrentStage;
        persisted.CurrentPlan = null;
        persisted.HeartbeatAt = DateTimeOffset.UtcNow;
        persisted.CompletedAt = failed.CompletedAt;
        persisted.LeaseId = null;
        persisted.LeaseExpiresAt = null;
        recovery.Store(persisted);
        await recovery.SaveChangesAsync(CancellationToken.None);
    }

    private static string ExtensionFor(string mimeType) => mimeType switch
    {
        "audio/mpeg" => ".mp3", "audio/ogg" => ".ogg", "audio/opus" => ".opus",
        "audio/flac" or "audio/x-flac" => ".flac", "audio/mp4" => ".m4a",
        "audio/aac" => ".aac", "audio/x-ms-wma" => ".wma", _ => ".wav",
    };

    public static string LocalizeGroup(string kind) => kind switch
    {
        "Keys" => "Клавиши", "Guitar" => "Гитары", "Bass" => "Бас",
        "Drums" => "Ударные", "Vocal" => "Вокал", "Click" => "Клик",
        "Guide" => "Гайд", "Backing" => "Бэкинг", _ => "Другое",
    };

    internal static string ArtifactKey(MixArtifact artifact) => ArtifactKey(artifact.Kind,
        artifact.TargetStemId, artifact.Format);

    internal static string ArtifactKey(MixArtifactKind kind, Guid? targetStemId, string format) =>
        MixProgress.Key(kind, targetStemId, format);

    private static MixArtifactKind ToArtifactKind(MixPlanKind kind) => kind switch
    {
        MixPlanKind.Focus => MixArtifactKind.Focus,
        MixPlanKind.Minus => MixArtifactKind.Minus,
        _ => MixArtifactKind.Full,
    };
}
