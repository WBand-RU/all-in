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
    ILogger<GenerateSongMixesHandler> logger)
{
    public async Task Handle(GenerateSongMixes command, IDocumentSession session,
        IMessageBus bus, CancellationToken cancellationToken)
    {
        using var generationLease = await MixGenerationLock.AcquireAsync(cancellationToken);
        var batch = await session.LoadAsync<MixBatch>(command.BatchId, cancellationToken);
        if (batch is null || batch.Status == MixBatchStatus.Ready) return;
        var startedAt = DateTimeOffset.UtcNow;
        batch.Status = MixBatchStatus.Processing;
        batch.StartedAt ??= startedAt;
        batch.HeartbeatAt = startedAt;
        batch.AttemptCount++;
        batch.Error = null;
        batch.CurrentStage = "Starting";
        batch.CurrentPlan = null;
        session.Store(batch); await session.SaveChangesAsync(cancellationToken);
        var workDirectory = Path.Combine(Path.GetTempPath(), "wband-mixer", batch.Id.ToString("N"));
        try
        {
            Directory.CreateDirectory(workDirectory);
            var sources = await bus.InvokeAsync<ReadyStemForMix[]>(new GetReadyStemsForMix(command.SongId));
            if (sources.Length == 0) throw new InvalidOperationException("No ready stems were found.");
            var downloads = (await bus.InvokeAsync<FileDownloadAccess[]>(new GetReadyFileDownloads(
                sources.Select(source => source.FileId).ToArray()))).ToDictionary(file => file.FileId);
            if (downloads.Count != sources.Length)
                throw new InvalidOperationException("Some stems are not ready for mixing.");
            var existingArtifacts = await session.Query<MixArtifact>()
                .Where(artifact => artifact.BatchId == batch.Id).ToListAsync(cancellationToken);
            var completedOutputs = existingArtifacts.Select(ArtifactKey).ToHashSet();
            batch.CompletedOutputCount = completedOutputs.Count;
            batch.CurrentStage = "Downloading sources";
            session.Store(batch);
            await session.SaveChangesAsync(cancellationToken);
            var client = httpClientFactory.CreateClient("mixer");
            var tracks = new List<AudioTrack>(sources.Length);
            foreach (var source in sources)
            {
                var access = downloads[source.FileId];
                var extension = ExtensionFor(access.MimeType);
                var path = Path.Combine(workDirectory, $"{source.StemId:N}{extension}");
                await using var input = await client.GetStreamAsync(access.DownloadUrl, cancellationToken);
                await using var output = File.Create(path);
                await input.CopyToAsync(output, cancellationToken);
                tracks.Add(new AudioTrack(source.StemId, path, source.Name, LocalizeGroup(source.Kind)));
            }
            var plans = MixPlanFactory.Create(tracks);
            batch.TotalOutputCount = plans.Count * 2;
            batch.CurrentStage = "Mixing";
            session.Store(batch);
            await session.SaveChangesAsync(cancellationToken);
            foreach (var plan in plans)
            {
                var artifactKind = ToArtifactKind(plan.Kind);
                var outputs = new[] { (Mime: "audio/wav", Format: "wav"),
                    (Mime: "audio/mpeg", Format: "mp3") };
                var missingOutputs = outputs.Where(output => !completedOutputs.Contains(
                    ArtifactKey(artifactKind, plan.Target?.StemId, output.Format))).ToArray();
                if (missingOutputs.Length == 0) continue;

                batch.HeartbeatAt = DateTimeOffset.UtcNow;
                batch.CurrentPlan = plan.Name;
                session.Store(batch);
                await session.SaveChangesAsync(cancellationToken);
                var wavePath = Path.Combine(workDirectory, $"{plan.Name}.wav");
                var mp3Path = Path.Combine(workDirectory, $"{plan.Name}.mp3");
                await FfmpegMixRunner.RunAsync(plan, wavePath, mp3Path, cancellationToken);
                foreach (var output in missingOutputs.Select(output => (Path:
                    output.Format == "wav" ? wavePath : mp3Path, output.Mime, output.Format)))
                {
                    var bytes = await File.ReadAllBytesAsync(output.Path, cancellationToken);
                    var fileId = Guid.CreateVersion7();
                    var checksum = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
                    var key = $"bands/{command.BandId:D}/songs/{command.SongId:D}/mixes/{fileId:D}/{plan.Name}.{output.Format}";
                    var upload = await bus.InvokeAsync<CreateFileUploadResult>(new CreateFileUpload(fileId,
                        command.BandId, key, $"{plan.Name}.{output.Format}", output.Mime,
                        bytes.LongLength, checksum, command.RequestedBy));
                    if (upload.Ticket is not { } ticket)
                        throw new InvalidOperationException($"Mix upload rejected: {upload.Error}");
                    using var content = new ByteArrayContent(bytes);
                    content.Headers.ContentType = new MediaTypeHeaderValue(output.Mime);
                    content.Headers.ContentLength = bytes.LongLength;
                    using var response = await client.PutAsync(ticket.UploadUrl, content, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    var completed = await bus.InvokeAsync<CompleteFileUploadResult>(new CompleteFileUpload(fileId));
                    if (completed.File is null)
                        throw new InvalidOperationException($"Mix verification failed: {completed.Error}");
                    session.Store(new MixArtifact { Id = Guid.CreateVersion7(), BatchId = batch.Id,
                        SongId = command.SongId, BandId = command.BandId, FileId = fileId,
                        Kind = artifactKind,
                        TargetStemId = plan.Target?.StemId, Group = plan.Target?.Group ?? "Общий микс",
                        Name = plan.Target is null ? "Полный микс" : plan.Target.Name,
                        Format = output.Format, CreatedAt = DateTimeOffset.UtcNow });
                    completedOutputs.Add(ArtifactKey(artifactKind, plan.Target?.StemId,
                        output.Format));
                    batch.CompletedOutputCount = completedOutputs.Count;
                    batch.HeartbeatAt = DateTimeOffset.UtcNow;
                    session.Store(batch);
                    await session.SaveChangesAsync(cancellationToken);
                }
            }
            batch.Status = MixBatchStatus.Ready; batch.CompletedAt = DateTimeOffset.UtcNow;
            batch.HeartbeatAt = batch.CompletedAt;
            batch.CurrentStage = "Ready";
            batch.CurrentPlan = null;
            session.Store(batch); await session.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            batch.Status = MixBatchStatus.Queued;
            batch.HeartbeatAt = DateTimeOffset.UtcNow;
            batch.CurrentStage = "Queued";
            session.Store(batch);
            try { await session.SaveChangesAsync(CancellationToken.None); }
            catch (Exception exception) { logger.LogWarning(exception,
                "Could not return cancelled mix batch {BatchId} to the queue", batch.Id); }
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Mix batch {BatchId} failed", batch.Id);
            batch.Status = MixBatchStatus.Failed; batch.Error = exception.Message;
            batch.CurrentStage = "Failed";
            batch.CompletedAt = DateTimeOffset.UtcNow; session.Store(batch);
            await session.SaveChangesAsync(CancellationToken.None);
        }
        finally
        {
            try { if (Directory.Exists(workDirectory)) Directory.Delete(workDirectory, true); }
            catch (IOException exception) { logger.LogWarning(exception,
                "Could not remove mixer working directory {Directory}", workDirectory); }
        }
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
        $"{kind}:{targetStemId?.ToString("D") ?? "full"}:{format.ToLowerInvariant()}";

    private static MixArtifactKind ToArtifactKind(MixPlanKind kind) => kind switch
    {
        MixPlanKind.Focus => MixArtifactKind.Focus,
        MixPlanKind.Minus => MixArtifactKind.Minus,
        _ => MixArtifactKind.Full,
    };
}
