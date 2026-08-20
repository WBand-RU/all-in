using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Shared;
using Shared.Services;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.SongModule.Application;
using WBand.Modules.SongModule.Contracts;
using WBand.Modules.SongModule.Domain;
using Wolverine;
using Wolverine.Http;

namespace WBand.Modules.SongModule.Endpoints;

public sealed record SongVersionResponse(
    long ContentVersion,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    string? ChangedByEmail,
    string? ChangedByRole);

public sealed record SongVersionDetailsResponse(
    long ContentVersion,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    string Title,
    IReadOnlyList<string> Authors,
    string? Key,
    int? Bpm,
    IReadOnlyList<TempoChange> TempoTrack,
    IReadOnlyList<TimeSignatureChange> TimeSignatureTrack,
    int CountInBars,
    SongStatus Status);

public static class ListSongVersionsEndpoint
{
    [Authorize]
    [WolverineGet("/song-api/songs/{songId}/versions")]
    public static async Task<IResult> Get(Guid songId, ICurrentUser user, IQuerySession session,
        IMessageBus bus, CancellationToken cancellationToken)
    {
        var song = await session.LoadAsync<Song>(songId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(bus, song.BandId, user.GetUserId, BandPermission.View)) return Results.Forbid();
        var versions = await session.Query<SongRevision>().Where(x => x.SongId == songId)
            .OrderByDescending(x => x.ContentVersion).ToListAsync(cancellationToken);
        var members = new Dictionary<Guid, BandMemberInfo>();
        foreach (var userId in versions.Select(x => x.CreatedBy).Distinct())
            members[userId] = await bus.InvokeAsync<BandMemberInfo>(new GetBandMemberInfo(song.BandId, userId));

        return Results.Ok(ApiResponse<IReadOnlyList<SongVersionResponse>>.Success(
            versions.Select(x =>
            {
                var member = members[x.CreatedBy];
                return new SongVersionResponse(
                    x.ContentVersion, x.CreatedAt, x.CreatedBy,
                    member.Found ? member.Email : null,
                    member.Found ? member.Role : null);
            }).ToList()));
    }
}

public static class GetSongVersionEndpoint
{
    [Authorize]
    [WolverineGet("/song-api/songs/{songId}/versions/{contentVersion}")]
    public static async Task<IResult> Get(
        Guid songId,
        long contentVersion,
        ICurrentUser user,
        IQuerySession session,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var song = await session.LoadAsync<Song>(songId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(bus, song.BandId, user.GetUserId, BandPermission.View))
            return Results.Forbid();

        var revision = await session.Query<SongRevision>().FirstOrDefaultAsync(
            x => x.SongId == songId && x.ContentVersion == contentVersion,
            cancellationToken);
        if (revision is null) return Results.NotFound();

        var value = revision.Snapshot;
        return Results.Ok(ApiResponse<SongVersionDetailsResponse>.Success(new(
            revision.ContentVersion,
            revision.CreatedAt,
            revision.CreatedBy,
            value.Title,
            value.Authors,
            value.Key,
            value.Bpm,
            value.TempoTrack,
            value.TimeSignatureTrack,
            value.CountInBars,
            value.Status)));
    }
}

public static class RestoreSongVersionEndpoint
{
    [Authorize]
    [WolverinePost("/song-api/songs/{songId}/versions/{contentVersion}/restore")]
    public static async Task<IResult> Post(Guid songId, long contentVersion, ICurrentUser user,
        IDocumentSession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        var song = await session.LoadAsync<Song>(songId, cancellationToken);
        if (song is null || song.IsDeleted) return Results.NotFound();
        if (!await SongAccess.Check(bus, song.BandId, user.GetUserId, BandPermission.EditContent)) return Results.Forbid();
        var revision = await session.Query<SongRevision>()
            .FirstOrDefaultAsync(x => x.SongId == songId && x.ContentVersion == contentVersion, cancellationToken);
        if (revision is null) return Results.NotFound();
        SongVersioning.Apply(song, revision.Snapshot);
        song.ContentVersion++; song.UpdatedAt = DateTimeOffset.UtcNow; song.UpdatedBy = user.GetUserId;
        session.Store(song);
        session.Store(SongVersioning.Revision(song, user.GetUserId, song.UpdatedAt.Value));
        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new SongChanged(song.Id, song.BandId, song.ContentVersion));
        return Results.Ok(ApiResponse<SongResponse>.Success(SongResponse.From(song)));
    }
}

public static class ListDeletedSongsEndpoint
{
    [Authorize]
    [WolverineGet("/song-api/songs/trash")]
    public static async Task<IResult> Get(Guid bandId, ICurrentUser user, IQuerySession session,
        IMessageBus bus, CancellationToken cancellationToken)
    {
        if (!await SongAccess.Check(bus, bandId, user.GetUserId, BandPermission.EditContent)) return Results.Forbid();
        var songs = await session.Query<Song>().Where(x => x.BandId == bandId && x.DeletedAt != null)
            .OrderByDescending(x => x.DeletedAt).ToListAsync(cancellationToken);
        return Results.Ok(ApiResponse<IReadOnlyList<SongResponse>>.Success(songs.Select(SongResponse.From).ToList()));
    }
}

public static class RestoreDeletedSongEndpoint
{
    [Authorize]
    [WolverinePost("/song-api/songs/{songId}/restore")]
    public static async Task<IResult> Post(Guid songId, ICurrentUser user, IDocumentSession session,
        IMessageBus bus, CancellationToken cancellationToken)
    {
        var song = await session.LoadAsync<Song>(songId, cancellationToken);
        if (song is null || !song.IsDeleted || song.PurgeAfter <= DateTimeOffset.UtcNow) return Results.NotFound();
        if (!await SongAccess.Check(bus, song.BandId, user.GetUserId, BandPermission.EditContent)) return Results.Forbid();
        song.DeletedAt = null; song.DeletedBy = null; song.ContentVersion++;
        song.UpdatedAt = DateTimeOffset.UtcNow; song.UpdatedBy = user.GetUserId;
        session.Store(song);
        session.Store(SongVersioning.Revision(song, user.GetUserId, song.UpdatedAt.Value));
        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new SongRestored(song.Id, song.BandId, song.ContentVersion));
        return Results.Ok(ApiResponse<SongResponse>.Success(SongResponse.From(song)));
    }
}
