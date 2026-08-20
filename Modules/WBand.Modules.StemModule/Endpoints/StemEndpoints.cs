using FluentValidation;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Shared.Services;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.FileModule.Contracts;
using WBand.Modules.SongModule.Contracts;
using WBand.Modules.StemModule.Contracts;
using WBand.Modules.StemModule.Domain;
using Wolverine;
using Wolverine.Http;

namespace WBand.Modules.StemModule.Endpoints;

public sealed record CreateStemRequest(
    string Name,
    StemKind Kind,
    Guid? GroupId,
    string FileName,
    string MimeType,
    long Size,
    string Sha256,
    int? DurationMilliseconds = null,
    int? SampleRateHz = null,
    int? Channels = null,
    int? BitDepth = null);

public sealed record UpdateStemRequest(
    string Name,
    StemKind Kind,
    Guid? GroupId,
    long ExpectedRevision,
    int? DurationMilliseconds = null,
    int? SampleRateHz = null,
    int? Channels = null,
    int? BitDepth = null);

public sealed record AssignStemGroupRequest(Guid? GroupId, long ExpectedRevision);
public sealed record CreateStemGroupRequest(string Name, int Order = 0);
public sealed record ReorderStemItem(Guid StemId, Guid? GroupId, int Order,
    long ExpectedRevision);
public sealed record ReorderStemsRequest(long ExpectedContentVersion,
    IReadOnlyList<ReorderStemItem> Stems);
public sealed record ReorderStemsResponse(long ContentVersion);

public sealed record StemGroupResponse(Guid Id, string Name, int Order);
public sealed record StemResponse(Guid Id, Guid SongId, Guid? GroupId, Guid FileId,
    string Name, StemKind Kind, int Order, int Version, long Revision, FileObjectStatus FileStatus,
    string MimeType, long Size, string Sha256, int? DurationMilliseconds,
    int? SampleRateHz, int? Channels, int? BitDepth);
public sealed record StemCollectionResponse(Guid SongId, long ContentVersion,
    IReadOnlyList<StemGroupResponse> Groups, IReadOnlyList<StemResponse> Stems);
public sealed record CreateStemResponse(StemResponse Stem, string UploadUrl,
    DateTimeOffset UploadExpiresAt, long ContentVersion);
public sealed record CreateStemBatchRequest(IReadOnlyList<CreateStemRequest> Files);
public sealed record CreateStemBatchResponse(IReadOnlyList<CreateStemResponse> Files,
    long ContentVersion);

public sealed class CreateStemRequestValidator : AbstractValidator<CreateStemRequest>
{
    public CreateStemRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(120);
        RuleFor(request => request.DurationMilliseconds).GreaterThan(0).When(x => x.DurationMilliseconds.HasValue);
        RuleFor(request => request.SampleRateHz).InclusiveBetween(8_000, 384_000).When(x => x.SampleRateHz.HasValue);
        RuleFor(request => request.Channels).InclusiveBetween(1, 32).When(x => x.Channels.HasValue);
        RuleFor(request => request.BitDepth).Must(value => value is null or 16 or 24 or 32);
        RuleFor(request => request.FileName).NotEmpty().MaximumLength(255)
            .Must(AudioFileRules.IsSupportedFileName).WithMessage("Unsupported audio format.");
        RuleFor(request => request.MimeType).Must(AudioFileRules.IsSupportedMimeType)
            .WithMessage("Unsupported audio format.");
        RuleFor(request => request.Size).GreaterThan(0);
        RuleFor(request => request.Sha256).Matches("^[a-fA-F0-9]{64}$");
    }
}

public sealed class CreateStemBatchRequestValidator : AbstractValidator<CreateStemBatchRequest>
{
    public CreateStemBatchRequestValidator()
    {
        RuleFor(request => request.Files).NotEmpty().Must(files => files.Count <= 100)
            .WithMessage("A folder may contain at most 100 audio files.");
        RuleForEach(request => request.Files).SetValidator(new CreateStemRequestValidator());
    }
}

public static class AudioFileRules
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        { ".wav", ".mp3", ".ogg", ".opus", ".flac", ".m4a", ".aac", ".wma" };
    private static readonly HashSet<string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
        { "audio/wav", "audio/x-wav", "audio/mpeg", "audio/ogg", "audio/opus",
          "audio/flac", "audio/x-flac", "audio/mp4", "audio/aac", "audio/x-ms-wma" };

    public static bool IsSupportedFileName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && Extensions.Contains(Path.GetExtension(name));
    public static bool IsSupportedMimeType(string? mime) => MimeTypes.Contains(
        (mime ?? string.Empty).Split(';', 2)[0].Trim());
    public static string SafeExtension(string fileName) =>
        Extensions.Contains(Path.GetExtension(fileName))
            ? Path.GetExtension(fileName).ToLowerInvariant()
            : ".bin";
}

public sealed class UpdateStemRequestValidator : AbstractValidator<UpdateStemRequest>
{
    public UpdateStemRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(120);
        RuleFor(request => request.ExpectedRevision).GreaterThan(0);
        RuleFor(request => request.DurationMilliseconds).GreaterThan(0).When(x => x.DurationMilliseconds.HasValue);
        RuleFor(request => request.SampleRateHz).InclusiveBetween(8_000, 384_000).When(x => x.SampleRateHz.HasValue);
        RuleFor(request => request.Channels).InclusiveBetween(1, 32).When(x => x.Channels.HasValue);
        RuleFor(request => request.BitDepth).Must(value => value is null or 16 or 24 or 32);
    }
}

public sealed class AssignStemGroupRequestValidator : AbstractValidator<AssignStemGroupRequest>
{
    public AssignStemGroupRequestValidator() =>
        RuleFor(request => request.ExpectedRevision).GreaterThan(0);
}

public sealed class CreateStemGroupRequestValidator : AbstractValidator<CreateStemGroupRequest>
{
    public CreateStemGroupRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(80);
        RuleFor(request => request.Order).GreaterThanOrEqualTo(0);
    }
}

public sealed class ReorderStemsRequestValidator : AbstractValidator<ReorderStemsRequest>
{
    public ReorderStemsRequestValidator()
    {
        RuleFor(request => request.ExpectedContentVersion).GreaterThanOrEqualTo(0);
        RuleFor(request => request.Stems).NotEmpty().Must(stems => stems.Count <= 1_000)
            .WithMessage("At most 1000 stems can be reordered at once.")
            .Must(stems => stems.Select(stem => stem.StemId).Distinct().Count() == stems.Count)
            .WithMessage("Stem identifiers must be unique.");
        RuleForEach(request => request.Stems).ChildRules(stem =>
        {
            stem.RuleFor(item => item.Order).GreaterThanOrEqualTo(0);
            stem.RuleFor(item => item.ExpectedRevision).GreaterThan(0);
        });
    }
}

internal static class StemEndpointSupport
{
    public static async Task<SongReference?> GetSong(IMessageBus bus, Guid songId)
    {
        var songs = await bus.InvokeAsync<SongReference[]>(new GetSongReferences([songId]));
        return songs.SingleOrDefault();
    }

    public static Task<bool> Can(IMessageBus bus, SongReference song, Guid userId,
        BandPermission permission) => bus.InvokeAsync<bool>(new CheckBandPermission(
            song.BandId, userId, permission));

    public static async Task<StemCollection> Touch(IDocumentSession session, SongReference song,
        CancellationToken cancellationToken)
    {
        var collection = await session.LoadAsync<StemCollection>(song.SongId, cancellationToken);
        if (collection is null)
        {
            collection = new StemCollection { Id = song.SongId, SongId = song.SongId,
                BandId = song.BandId };
        }
        else collection.ContentVersion++;
        session.Store(collection);
        return collection;
    }

    public static async Task<bool> GroupBelongsToSong(IQuerySession session, Guid? groupId,
        Guid songId, CancellationToken cancellationToken) => groupId is null ||
        (await session.LoadAsync<StemGroup>(groupId.Value, cancellationToken))?.SongId == songId;

    public static StemResponse Response(Stem stem, FileReference? file) => new(stem.Id,
        stem.SongId, stem.GroupId, stem.FileId, stem.Name, stem.Kind, stem.Order, stem.Version,
        stem.Revision, file?.Status ?? FileObjectStatus.Pending, file?.MimeType ?? "audio/wav",
        file?.Size ?? 0, file?.Sha256 ?? string.Empty, stem.DurationMilliseconds,
        stem.SampleRateHz, stem.Channels, stem.BitDepth);

    public static int NextOrder(IEnumerable<Stem> stems, Guid? groupId) => stems
        .Where(stem => stem.GroupId == groupId).Select(stem => stem.Order)
        .DefaultIfEmpty(-1).Max() + 1;
}

public static class ListStemsEndpoint
{
    [Authorize]
    [WolverineGet("/stem-api/songs/{songId}/stems")]
    public static async Task<IResult> Get(Guid songId, ICurrentUser user, IQuerySession session,
        IMessageBus bus, CancellationToken cancellationToken)
    {
        var song = await StemEndpointSupport.GetSong(bus, songId);
        if (song is null) return Results.NotFound();
        if (!await StemEndpointSupport.Can(bus, song, user.GetUserId, BandPermission.View)) return Results.Forbid();
        var stems = (await session.Query<Stem>().Where(stem => stem.SongId == songId)
            .ToListAsync(cancellationToken)).OrderBy(stem => stem.GroupId)
            .ThenBy(stem => stem.Order).ThenBy(stem => stem.Name).ToArray();
        var groups = await session.Query<StemGroup>().Where(group => group.SongId == songId)
            .OrderBy(group => group.Order).ToListAsync(cancellationToken);
        var files = (await bus.InvokeAsync<FileReference[]>(new GetFileReferences(
            stems.Select(stem => stem.FileId).ToArray()))).ToDictionary(file => file.FileId);
        var collection = await session.LoadAsync<StemCollection>(songId, cancellationToken);
        return Results.Ok(new StemCollectionResponse(songId, collection?.ContentVersion ?? 0,
            groups.Select(group => new StemGroupResponse(group.Id, group.Name, group.Order)).ToArray(),
            stems.Select(stem => StemEndpointSupport.Response(stem,
                files.GetValueOrDefault(stem.FileId))).ToArray()));
    }
}

public static class CreateStemEndpoint
{
    [Authorize]
    [WolverinePost("/stem-api/songs/{songId}/stems")]
    public static async Task<IResult> Post(Guid songId, CreateStemRequest request,
        ICurrentUser user, IDocumentSession session, IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var song = await StemEndpointSupport.GetSong(bus, songId);
        if (song is null) return Results.NotFound();
        if (!await StemEndpointSupport.Can(bus, song, user.GetUserId, BandPermission.EditContent)) return Results.Forbid();
        if (!await StemEndpointSupport.GroupBelongsToSong(session, request.GroupId, songId, cancellationToken))
            return Results.BadRequest(new { code = "stem_group_outside_song" });
        var existingStems = await session.Query<Stem>().Where(stem => stem.SongId == songId)
            .ToListAsync(cancellationToken);

        var stemId = Guid.CreateVersion7();
        var key = $"bands/{song.BandId:D}/songs/{songId:D}/stems/{stemId:D}/1{AudioFileRules.SafeExtension(request.FileName)}";
        var upload = await bus.InvokeAsync<CreateFileUploadResult>(new CreateFileUpload(stemId,
            song.BandId, key, request.FileName, request.MimeType, request.Size, request.Sha256,
            user.GetUserId));
        if (upload.Ticket is not { } ticket)
            return Results.BadRequest(new { code = upload.Error });
        var stem = new Stem { Id = stemId, SongId = songId, BandId = song.BandId,
            GroupId = request.GroupId, FileId = ticket.FileId, Name = request.Name.Trim(),
            Kind = request.Kind, Order = StemEndpointSupport.NextOrder(existingStems, request.GroupId),
            DurationMilliseconds = request.DurationMilliseconds,
            SampleRateHz = request.SampleRateHz, Channels = request.Channels,
            BitDepth = request.BitDepth, CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = user.GetUserId };
        var collection = await StemEndpointSupport.Touch(session, song, cancellationToken);
        session.Store(stem);
        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new StemsChanged(songId, song.BandId, collection.ContentVersion));
        var file = new FileReference(ticket.FileId, song.BandId, FileObjectStatus.Pending,
            request.MimeType, request.Size, request.Sha256);
        return Results.Ok(new CreateStemResponse(StemEndpointSupport.Response(stem, file),
            ticket.UploadUrl, ticket.ExpiresAt, collection.ContentVersion));
    }
}

public static class CreateStemBatchEndpoint
{
    [Authorize]
    [WolverinePost("/stem-api/songs/{songId}/stems/batch")]
    public static async Task<IResult> Post(Guid songId, CreateStemBatchRequest request,
        ICurrentUser user, IDocumentSession session, IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var song = await StemEndpointSupport.GetSong(bus, songId);
        if (song is null) return Results.NotFound();
        if (!await StemEndpointSupport.Can(bus, song, user.GetUserId, BandPermission.EditContent))
            return Results.Forbid();

        var now = DateTimeOffset.UtcNow;
        var results = new List<CreateStemResponse>(request.Files.Count);
        var existingStems = (await session.Query<Stem>().Where(stem => stem.SongId == songId)
            .ToListAsync(cancellationToken)).ToList();
        foreach (var file in request.Files)
        {
            if (!await StemEndpointSupport.GroupBelongsToSong(session, file.GroupId, songId,
                cancellationToken)) return Results.BadRequest(new { code = "stem_group_outside_song" });
            var stemId = Guid.CreateVersion7();
            var key = $"bands/{song.BandId:D}/songs/{songId:D}/stems/{stemId:D}/1{AudioFileRules.SafeExtension(file.FileName)}";
            var upload = await bus.InvokeAsync<CreateFileUploadResult>(new CreateFileUpload(stemId,
                song.BandId, key, file.FileName, file.MimeType, file.Size, file.Sha256,
                user.GetUserId));
            if (upload.Ticket is not { } ticket)
                return Results.BadRequest(new { code = upload.Error, fileName = file.FileName });
            var stem = new Stem { Id = stemId, SongId = songId, BandId = song.BandId,
                GroupId = file.GroupId, FileId = ticket.FileId, Name = file.Name.Trim(),
                Kind = file.Kind, Order = StemEndpointSupport.NextOrder(existingStems, file.GroupId),
                DurationMilliseconds = file.DurationMilliseconds,
                SampleRateHz = file.SampleRateHz, Channels = file.Channels,
                BitDepth = file.BitDepth, CreatedAt = now, CreatedBy = user.GetUserId };
            session.Store(stem);
            existingStems.Add(stem);
            var reference = new FileReference(ticket.FileId, song.BandId, FileObjectStatus.Pending,
                file.MimeType, file.Size, file.Sha256);
            results.Add(new CreateStemResponse(StemEndpointSupport.Response(stem, reference),
                ticket.UploadUrl, ticket.ExpiresAt, 0));
        }
        var collection = await StemEndpointSupport.Touch(session, song, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new StemsChanged(songId, song.BandId, collection.ContentVersion));
        return Results.Ok(new CreateStemBatchResponse(results.Select(result =>
            result with { ContentVersion = collection.ContentVersion }).ToArray(),
            collection.ContentVersion));
    }
}

public static class ReorderStemsEndpoint
{
    [Authorize]
    [WolverinePut("/stem-api/songs/{songId}/stems/order")]
    public static async Task<IResult> Put(Guid songId, ReorderStemsRequest request,
        ICurrentUser user, IDocumentSession session, IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var song = await StemEndpointSupport.GetSong(bus, songId);
        if (song is null) return Results.NotFound();
        if (!await StemEndpointSupport.Can(bus, song, user.GetUserId,
            BandPermission.EditContent)) return Results.Forbid();

        var collection = await session.LoadAsync<StemCollection>(songId, cancellationToken);
        var currentContentVersion = collection?.ContentVersion ?? 0;
        if (currentContentVersion != request.ExpectedContentVersion)
            return Results.Conflict(new { code = "stem_collection_version_conflict",
                currentContentVersion });

        var requestedIds = request.Stems.Select(item => item.StemId).ToArray();
        var stems = await session.Query<Stem>().Where(stem => requestedIds.Contains(stem.Id))
            .ToListAsync(cancellationToken);
        if (stems.Count != requestedIds.Length || stems.Any(stem => stem.SongId != songId))
            return Results.BadRequest(new { code = "stem_outside_song" });

        var groupIds = request.Stems.Where(item => item.GroupId.HasValue)
            .Select(item => item.GroupId!.Value).Distinct().ToArray();
        var validGroupIds = (await session.Query<StemGroup>()
            .Where(group => group.SongId == songId && groupIds.Contains(group.Id))
            .ToListAsync(cancellationToken)).Select(group => group.Id).ToHashSet();
        if (validGroupIds.Count != groupIds.Length)
            return Results.BadRequest(new { code = "stem_group_outside_song" });

        var changes = request.Stems.ToDictionary(item => item.StemId);
        if (stems.Any(stem => stem.Revision != changes[stem.Id].ExpectedRevision))
            return Results.Conflict(new { code = "stem_version_conflict" });

        var now = DateTimeOffset.UtcNow;
        foreach (var stem in stems)
        {
            var change = changes[stem.Id];
            stem.GroupId = change.GroupId;
            stem.Order = change.Order;
            stem.Revision++;
            stem.UpdatedAt = now;
            stem.UpdatedBy = user.GetUserId;
            session.Store(stem);
        }

        collection = await StemEndpointSupport.Touch(session, song, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new StemsChanged(songId, song.BandId,
            collection.ContentVersion));
        return Results.Ok(new ReorderStemsResponse(collection.ContentVersion));
    }
}

public static class UpdateStemEndpoint
{
    [Authorize]
    [WolverinePut("/stem-api/stems/{stemId}")]
    public static async Task<IResult> Put(Guid stemId, UpdateStemRequest request,
        ICurrentUser user, IDocumentSession session, IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var stem = await session.LoadAsync<Stem>(stemId, cancellationToken);
        if (stem is null) return Results.NotFound();
        var song = await StemEndpointSupport.GetSong(bus, stem.SongId);
        if (song is null) return Results.NotFound();
        if (!await StemEndpointSupport.Can(bus, song, user.GetUserId, BandPermission.EditContent)) return Results.Forbid();
        if (stem.Revision != request.ExpectedRevision)
            return Results.Conflict(new { code = "stem_version_conflict", currentRevision = stem.Revision });
        if (!await StemEndpointSupport.GroupBelongsToSong(session, request.GroupId, stem.SongId, cancellationToken))
            return Results.BadRequest(new { code = "stem_group_outside_song" });
        stem.Name = request.Name.Trim(); stem.Kind = request.Kind; stem.GroupId = request.GroupId;
        stem.DurationMilliseconds = request.DurationMilliseconds; stem.SampleRateHz = request.SampleRateHz;
        stem.Channels = request.Channels; stem.BitDepth = request.BitDepth; stem.Revision++;
        stem.UpdatedAt = DateTimeOffset.UtcNow; stem.UpdatedBy = user.GetUserId;
        var collection = await StemEndpointSupport.Touch(session, song, cancellationToken);
        session.Store(stem); await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new StemsChanged(stem.SongId, stem.BandId, collection.ContentVersion));
        var files = await bus.InvokeAsync<FileReference[]>(new GetFileReferences([stem.FileId]));
        return Results.Ok(StemEndpointSupport.Response(stem, files.SingleOrDefault()));
    }
}

public static class AssignStemGroupEndpoint
{
    [Authorize]
    [WolverinePost("/stem-api/stems/{stemId}/group")]
    public static async Task<IResult> Post(Guid stemId, AssignStemGroupRequest request,
        ICurrentUser user, IDocumentSession session, IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var stem = await session.LoadAsync<Stem>(stemId, cancellationToken);
        if (stem is null) return Results.NotFound();
        var song = await StemEndpointSupport.GetSong(bus, stem.SongId);
        if (song is null) return Results.NotFound();
        if (!await StemEndpointSupport.Can(bus, song, user.GetUserId,
            BandPermission.EditContent)) return Results.Forbid();
        if (stem.Revision != request.ExpectedRevision)
            return Results.Conflict(new { code = "stem_version_conflict",
                currentRevision = stem.Revision });
        if (!await StemEndpointSupport.GroupBelongsToSong(session, request.GroupId,
            stem.SongId, cancellationToken))
            return Results.BadRequest(new { code = "stem_group_outside_song" });
        stem.GroupId = request.GroupId; stem.Revision++;
        stem.UpdatedAt = DateTimeOffset.UtcNow; stem.UpdatedBy = user.GetUserId;
        var collection = await StemEndpointSupport.Touch(session, song, cancellationToken);
        session.Store(stem); await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new StemsChanged(stem.SongId, stem.BandId,
            collection.ContentVersion));
        var files = await bus.InvokeAsync<FileReference[]>(new GetFileReferences([stem.FileId]));
        return Results.Ok(StemEndpointSupport.Response(stem, files.SingleOrDefault()));
    }
}

public static class CreateStemGroupEndpoint
{
    [Authorize]
    [WolverinePost("/stem-api/songs/{songId}/stem-groups")]
    public static async Task<IResult> Post(Guid songId, CreateStemGroupRequest request,
        ICurrentUser user, IDocumentSession session, IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var song = await StemEndpointSupport.GetSong(bus, songId);
        if (song is null) return Results.NotFound();
        if (!await StemEndpointSupport.Can(bus, song, user.GetUserId, BandPermission.EditContent)) return Results.Forbid();
        var group = new StemGroup { Id = Guid.CreateVersion7(), SongId = songId,
            BandId = song.BandId, Name = request.Name.Trim(), Order = request.Order,
            CreatedAt = DateTimeOffset.UtcNow, CreatedBy = user.GetUserId };
        var collection = await StemEndpointSupport.Touch(session, song, cancellationToken);
        session.Store(group); await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new StemsChanged(songId, song.BandId, collection.ContentVersion));
        return Results.Ok(new StemGroupResponse(group.Id, group.Name, group.Order));
    }
}
