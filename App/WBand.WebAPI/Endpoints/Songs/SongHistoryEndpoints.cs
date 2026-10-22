using Wolverine;
using WBand.Modules.BandModule.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WBand.WebAPI.Application.Songs;
using DataContext = WBand.Modules.SongModule.Data.SongDataContext;
using WBand.WebAPI.Services;
using WBand.WebAPI.Shared;
using Wolverine.Http;

namespace WBand.WebAPI.Endpoints.Songs;

public static class ListSongVersionsEndpoint
{
    [Authorize, Tags("SongHistory"), ProducesResponseType<ApiResponse<IReadOnlyList<SongVersionResponse>>>(200)]
    [WolverineGet("/song-api/songs/{songId}/versions", OperationId = "GetSongVersions")]
    public static async Task<IResult> Get(Guid songId, ICurrentUser user, [FromServices] DataContext db, [FromServices] IMessageBus bus, CancellationToken cancellationToken)
    {
        var song = await SongAccess.Load(db, songId, cancellationToken, includeDeleted: true);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, bus, song.BandId, user.Id, false, cancellationToken)) return Results.Forbid();
        var versions = await db.QueryAsync<VersionRow>("""
            select r.content_version, r.created_at, r.created_by
            from song_revisions r
            where r.song_id = @songId order by r.content_version desc
            """, new { songId }, cancellationToken);
        var contributors = (await bus.InvokeAsync<BandContributorInfo[]>(
            new GetBandContributorInfo(song.BandId, versions.Select(version => version.CreatedBy).Distinct().ToArray()), cancellationToken))
            .ToDictionary(contributor => contributor.UserId);
        return Results.Ok(ApiResponse<IReadOnlyList<SongVersionResponse>>.Success(versions.Select(version =>
        {
            contributors.TryGetValue(version.CreatedBy, out var contributor);
            return new SongVersionResponse(version.ContentVersion, version.CreatedAt, version.CreatedBy,
                contributor?.Email, contributor?.Role);
        }).ToArray()));
    }

    private sealed class VersionRow
    {
        [System.ComponentModel.DataAnnotations.Schema.Column("content_version")]
        public long ContentVersion { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.Column("created_by")]
        public Guid CreatedBy { get; set; }
    }
}

public static class GetSongVersionEndpoint
{
    [Authorize, Tags("SongHistory"), ProducesResponseType<ApiResponse<SongVersionDetailsResponse>>(200)]
    [WolverineGet("/song-api/songs/{songId}/versions/{contentVersion}", OperationId = "GetSongVersion")]
    public static async Task<IResult> Get(Guid songId, long contentVersion, ICurrentUser user,
        [FromServices] DataContext db, [FromServices] IMessageBus bus, CancellationToken cancellationToken)
    {
        var song = await SongAccess.Load(db, songId, cancellationToken, includeDeleted: true);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, bus, song.BandId, user.Id, false, cancellationToken)) return Results.Forbid();
        var revision = await db.SingleOrDefaultAsync<SongRevision>(
            "select * from song_revisions where song_id = @songId and content_version = @contentVersion", new { songId, contentVersion }, cancellationToken);
        if (revision is null) return Results.NotFound();
        return Results.Ok(ApiResponse<SongVersionDetailsResponse>.Success(new(revision.ContentVersion,
            revision.CreatedAt, revision.CreatedBy, revision.Title, revision.Authors, revision.Key, revision.Bpm, revision.Status)));
    }
}

public static class RestoreSongVersionEndpoint
{
    [Authorize, Tags("SongHistory"), ProducesResponseType<ApiResponse<SongResponse>>(200)]
    [WolverinePost("/song-api/songs/{songId}/versions/{contentVersion}/restore", OperationId = "RestoreSongVersion")]
    public static async Task<IResult> Post(Guid songId, long contentVersion, ICurrentUser user,
        [FromServices] DataContext db, [FromServices] IMessageBus bus, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        var song = await SongAccess.Load(db, songId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, bus, song.BandId, user.Id, true, cancellationToken)) return Results.Forbid();
        var revision = await db.SingleOrDefaultAsync<SongRevision>(
            "select * from song_revisions where song_id = @songId and content_version = @contentVersion", new { songId, contentVersion }, cancellationToken);
        if (revision is null) return Results.NotFound();
        SongVersioning.Apply(song, revision);
        SongVersioning.Changed(song, user.Id);
        await db.UpdateAsync(song, cancellationToken);
        await SongVersioning.SaveRevision(db, song, user.Id, song.UpdatedAt!.Value, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Ok(ApiResponse<SongResponse>.Success(SongResponse.From(song)));
    }
}

public static class ListDeletedSongsEndpoint
{
    [Authorize, Tags("SongHistory"), ProducesResponseType<ApiResponse<IReadOnlyList<SongResponse>>>(200)]
    [WolverineGet("/song-api/songs/trash", OperationId = "GetDeletedSongs")]
    public static async Task<IResult> Get(Guid bandId, ICurrentUser user, [FromServices] DataContext db, [FromServices] IMessageBus bus, CancellationToken cancellationToken)
    {
        if (!await SongAccess.Check(db, bus, bandId, user.Id, true, cancellationToken)) return Results.Forbid();
        var songs = await db.QueryAsync<Song>("select * from songs where band_id = @bandId and deleted_at is not null order by deleted_at desc, id", new { bandId }, cancellationToken);
        return Results.Ok(ApiResponse<IReadOnlyList<SongResponse>>.Success(songs.Select(SongResponse.From).ToArray()));
    }
}

public static class RestoreDeletedSongEndpoint
{
    [Authorize, Tags("SongHistory"), ProducesResponseType<ApiResponse<SongResponse>>(200)]
    [WolverinePost("/song-api/songs/{songId}/restore", OperationId = "RestoreDeletedSong")]
    public static async Task<IResult> Post(Guid songId, ICurrentUser user,
        [FromServices] DataContext db, [FromServices] IMessageBus bus, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        var song = await SongAccess.Load(db, songId, cancellationToken, includeDeleted: true);
        if (song is null || !song.IsDeleted || song.PurgeAfter <= DateTimeOffset.UtcNow) return Results.NotFound();
        if (!await SongAccess.Check(db, bus, song.BandId, user.Id, true, cancellationToken)) return Results.Forbid();
        song.DeletedAt = null;
        song.DeletedBy = null;
        SongVersioning.Changed(song, user.Id);
        await db.UpdateAsync(song, cancellationToken);
        await SongVersioning.SaveRevision(db, song, user.Id, song.UpdatedAt!.Value, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Ok(ApiResponse<SongResponse>.Success(SongResponse.From(song)));
    }
}
