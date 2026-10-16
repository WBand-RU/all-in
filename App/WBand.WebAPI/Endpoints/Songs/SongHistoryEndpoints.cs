using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WBand.Modules.SongModule.Contracts;
using WBand.WebAPI.Application.Songs;
using WBand.WebAPI.Data;
using Wolverine;
using WBand.WebAPI.Services;
using WBand.WebAPI.Shared;
using Wolverine.Http;

namespace WBand.WebAPI.Endpoints.Songs;

public static class ListSongVersionsEndpoint
{
    [Authorize, Tags("SongHistory"), ProducesResponseType<ApiResponse<IReadOnlyList<SongVersionResponse>>>(200)]
    [WolverineGet("/song-api/songs/{songId}/versions", OperationId = "GetSongVersions")]
    public static async Task<IResult> Get(Guid songId, ICurrentUser user, [FromServices] DataContext db, CancellationToken cancellationToken)
    {
        var song = await SongAccess.Load(db, songId, cancellationToken, includeDeleted: true);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, song.BandId, user.Id, false, cancellationToken)) return Results.Forbid();
        var versions = await db.QueryAsync<VersionRow>("""
            select r.content_version, r.created_at, r.created_by, u.email as changed_by_email, m.role
            from public.song_revisions r
            join public.users u on u.keycloak_id = r.created_by
            left join public.band_members m on m.band_id = @bandId and m.user_id = r.created_by
            where r.song_id = @songId order by r.content_version desc
            """, new { songId, bandId = song.BandId }, cancellationToken);
        return Results.Ok(ApiResponse<IReadOnlyList<SongVersionResponse>>.Success(versions.Select(x => new SongVersionResponse(
            x.ContentVersion, x.CreatedAt, x.CreatedBy, x.ChangedByEmail, x.Role?.ToString())).ToArray()));
    }

    private sealed class VersionRow
    {
        [System.ComponentModel.DataAnnotations.Schema.Column("content_version")]
        public long ContentVersion { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.Column("created_by")]
        public Guid CreatedBy { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.Column("changed_by_email")]
        public string? ChangedByEmail { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.Column("role")]
        public BandMemberRole? Role { get; set; }
    }
}

public static class GetSongVersionEndpoint
{
    [Authorize, Tags("SongHistory"), ProducesResponseType<ApiResponse<SongVersionDetailsResponse>>(200)]
    [WolverineGet("/song-api/songs/{songId}/versions/{contentVersion}", OperationId = "GetSongVersion")]
    public static async Task<IResult> Get(Guid songId, long contentVersion, ICurrentUser user,
        [FromServices] DataContext db, CancellationToken cancellationToken)
    {
        var song = await SongAccess.Load(db, songId, cancellationToken, includeDeleted: true);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, song.BandId, user.Id, false, cancellationToken)) return Results.Forbid();
        var revision = await db.SingleOrDefaultAsync<SongRevision>(
            "select * from public.song_revisions where song_id = @songId and content_version = @contentVersion", new { songId, contentVersion }, cancellationToken);
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
        if (!await SongAccess.Check(db, song.BandId, user.Id, true, cancellationToken)) return Results.Forbid();
        var revision = await db.SingleOrDefaultAsync<SongRevision>(
            "select * from public.song_revisions where song_id = @songId and content_version = @contentVersion", new { songId, contentVersion }, cancellationToken);
        if (revision is null) return Results.NotFound();
        SongVersioning.Apply(song, revision);
        SongVersioning.Changed(song, user.Id);
        await db.UpdateAsync(song, cancellationToken);
        await SongVersioning.SaveRevision(db, song, user.Id, song.UpdatedAt!.Value, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await bus.PublishAsync(new SongChanged(song.Id, song.BandId, song.ContentVersion));
        return Results.Ok(ApiResponse<SongResponse>.Success(SongResponse.From(song)));
    }
}

public static class ListDeletedSongsEndpoint
{
    [Authorize, Tags("SongHistory"), ProducesResponseType<ApiResponse<IReadOnlyList<SongResponse>>>(200)]
    [WolverineGet("/song-api/songs/trash", OperationId = "GetDeletedSongs")]
    public static async Task<IResult> Get(Guid bandId, ICurrentUser user, [FromServices] DataContext db, CancellationToken cancellationToken)
    {
        if (!await SongAccess.Check(db, bandId, user.Id, true, cancellationToken)) return Results.Forbid();
        var songs = await db.QueryAsync<Song>("select * from public.songs where band_id = @bandId and deleted_at is not null order by deleted_at desc, id", new { bandId }, cancellationToken);
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
        if (!await SongAccess.Check(db, song.BandId, user.Id, true, cancellationToken)) return Results.Forbid();
        song.DeletedAt = null;
        song.DeletedBy = null;
        SongVersioning.Changed(song, user.Id);
        await db.UpdateAsync(song, cancellationToken);
        await SongVersioning.SaveRevision(db, song, user.Id, song.UpdatedAt!.Value, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await bus.PublishAsync(new SongRestored(song.Id, song.BandId, song.ContentVersion));
        return Results.Ok(ApiResponse<SongResponse>.Success(SongResponse.From(song)));
    }
}
