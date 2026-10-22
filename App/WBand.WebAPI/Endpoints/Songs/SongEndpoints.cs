using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WBand.WebAPI.Application.Songs;
using WBand.WebAPI.Data;
using WBand.WebAPI.Services;
using WBand.WebAPI.Shared;
using Wolverine.Http;

namespace WBand.WebAPI.Endpoints.Songs;

public static class CreateSongEndpoint
{
    [Authorize, Tags("Songs"), ProducesResponseType<ApiResponse<SongResponse>>(200)]
    [WolverinePost("/song-api/songs", OperationId = "CreateSong")]
    public static async Task<IResult> Post(CreateSongRequest request, ICurrentUser user,
        [FromServices] DataContext db, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        if (!await SongAccess.Check(db, request.BandId, user.Id, true, cancellationToken)) return Results.Forbid();
        var song = new Song
        {
            Id = Guid.CreateVersion7(), BandId = request.BandId, Title = request.Title.Trim(),
            Authors = SongAccess.Authors(request.Authors, request.Author), Key = request.Key, Bpm = request.Bpm,
            CreatedAt = DateTimeOffset.UtcNow, CreatedBy = user.Id,
        };
        await db.InsertAsync(song, cancellationToken);
        await SongVersioning.SaveRevision(db, song, user.Id, song.CreatedAt, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Ok(ApiResponse<SongResponse>.Success(SongResponse.From(song)));
    }
}

public static class ListSongsEndpoint
{
    [Authorize, Tags("Songs"), ProducesResponseType<ApiResponse<TableResponse<SongResponse>>>(200)]
    [WolverineGet("/song-api/songs", OperationId = "GetListOfSongs")]
    public static async Task<IResult> Get(Guid? bandId, int page, int pageSize, string? searchTerm,
        ICurrentUser user, [FromServices] DataContext db, CancellationToken cancellationToken)
    {
        if (bandId.HasValue && !await SongAccess.Check(db, bandId.Value, user.Id, false, cancellationToken)) return Results.Forbid();
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        const string filter = """
            from public.songs s where s.deleted_at is null
            and (@bandId is null or s.band_id = @bandId)
            and exists (select 1 from public.band_members m where m.band_id = s.band_id and m.user_id = @userId)
            and (@search is null or strpos(lower(s.title), lower(@search)) > 0)
            """;
        var parameters = new { bandId, userId = user.Id, search = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(),
            offset = ((long)page - 1) * pageSize, pageSize };
        var total = await db.SingleOrDefaultAsync<long>("select count(*) " + filter, parameters, cancellationToken);
        var songs = await db.QueryAsync<Song>("select s.* " + filter + " order by s.title, s.id offset @offset limit @pageSize", parameters, cancellationToken);
        return Results.Ok(ApiResponse<TableResponse<SongResponse>>.Success(new(total, songs.Select(SongResponse.From))));
    }
}

public static class GetSongEndpoint
{
    [Authorize, Tags("Songs"), ProducesResponseType<ApiResponse<SongResponse>>(200)]
    [WolverineGet("/song-api/songs/{songId}", OperationId = "GetSong")]
    public static async Task<IResult> Get(Guid songId, ICurrentUser user, [FromServices] DataContext db, CancellationToken cancellationToken)
    {
        var song = await SongAccess.Load(db, songId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, song.BandId, user.Id, false, cancellationToken)) return Results.Forbid();
        return Results.Ok(ApiResponse<SongResponse>.Success(SongResponse.From(song)));
    }
}

public static class UpdateSongEndpoint
{
    [Authorize, Tags("Songs"), ProducesResponseType<ApiResponse<SongResponse>>(200)]
    [WolverinePut("/song-api/songs/{songId}", OperationId = "UpdateSong")]
    public static async Task<IResult> Put(Guid songId, UpdateSongRequest request, ICurrentUser user,
        [FromServices] DataContext db, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        var song = await SongAccess.Load(db, songId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, song.BandId, user.Id, true, cancellationToken)) return Results.Forbid();
        if (song.ContentVersion != request.ExpectedContentVersion)
            return Results.Conflict(new { code = "song_version_conflict", currentContentVersion = song.ContentVersion });
        song.Title = request.Title.Trim();
        song.Authors = SongAccess.Authors(request.Authors, request.Author);
        song.Key = request.Key;
        song.Bpm = request.Bpm;
        song.Status = request.Status;
        SongVersioning.Changed(song, user.Id);
        await db.UpdateAsync(song, cancellationToken);
        await SongVersioning.SaveRevision(db, song, user.Id, song.UpdatedAt!.Value, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Ok(ApiResponse<SongResponse>.Success(SongResponse.From(song)));
    }
}

public static class DeleteSongEndpoint
{
    [Authorize, Tags("Songs"), ProducesResponseType<ApiResponse>(200)]
    [WolverineDelete("/song-api/songs/{songId}", OperationId = "DeleteSong")]
    public static async Task<IResult> Delete(Guid songId, ICurrentUser user,
        [FromServices] DataContext db, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        var song = await SongAccess.Load(db, songId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, song.BandId, user.Id, true, cancellationToken)) return Results.Forbid();
        SongVersioning.Changed(song, user.Id);
        song.DeletedAt = song.UpdatedAt;
        song.DeletedBy = user.Id;
        await db.UpdateAsync(song, cancellationToken);
        await SongVersioning.SaveRevision(db, song, user.Id, song.UpdatedAt!.Value, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Ok(ApiResponse.Success());
    }
}
