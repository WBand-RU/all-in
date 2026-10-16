using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WBand.Modules.SongModule.Contracts;
using WBand.WebAPI.Application.Songs;
using WBand.WebAPI.Data;
using Wolverine;
using WBand.WebAPI.Services;
using Wolverine.Http;

namespace WBand.WebAPI.Endpoints.Songs;

public static class ListSongSectionsEndpoint
{
    [Authorize, Tags("SongSections"), ProducesResponseType<SongSectionResponse[]>(200)]
    [WolverineGet("/song-api/songs/{songId}/sections", OperationId = "GetSongSections")]
    public static async Task<IResult> Get(Guid songId, ICurrentUser user, [FromServices] DataContext db, CancellationToken cancellationToken)
    {
        var song = await SongAccess.Load(db, songId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, song.BandId, user.Id, false, cancellationToken)) return Results.Forbid();
        var sections = await db.QueryAsync<SongSection>(
            "select * from public.song_sections where song_id = @songId order by \"order\", created_at, id", new { songId }, cancellationToken);
        return Results.Ok(sections.Select(SongSectionResponse.From).ToArray());
    }
}

public static class CreateSongSectionEndpoint
{
    [Authorize, Tags("SongSections"), ProducesResponseType<SongSectionResponse>(200)]
    [WolverinePost("/song-api/songs/{songId}/sections", OperationId = "CreateSongSection")]
    public static async Task<IResult> Post(Guid songId, CreateSongSectionRequest request, ICurrentUser user,
        [FromServices] DataContext db, [FromServices] IMessageBus bus, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        var song = await SongAccess.Load(db, songId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, song.BandId, user.Id, true, cancellationToken)) return Results.Forbid();
        var section = new SongSection
        {
            Id = Guid.CreateVersion7(), SongId = songId, Name = request.Name.Trim(), Order = request.Order,
            Lyrics = request.Lyrics, Chords = request.Chords, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = user.Id,
        };
        await db.InsertAsync(section, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await bus.PublishAsync(new SongSectionsChanged(songId, song.BandId));
        return Results.Ok(SongSectionResponse.From(section));
    }
}

public static class UpdateSongSectionEndpoint
{
    [Authorize, Tags("SongSections"), ProducesResponseType<SongSectionResponse>(200)]
    [WolverinePut("/song-api/sections/{sectionId}", OperationId = "UpdateSongSection")]
    public static async Task<IResult> Put(Guid sectionId, UpdateSongSectionRequest request, ICurrentUser user,
        [FromServices] DataContext db, [FromServices] IMessageBus bus, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        var section = await db.FindAsync<SongSection>(sectionId, cancellationToken);
        if (section is null) return Results.NotFound();
        var song = await SongAccess.Load(db, section.SongId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, song.BandId, user.Id, true, cancellationToken)) return Results.Forbid();
        section = await db.FindAsync<SongSection>(sectionId, cancellationToken);
        if (section is null) return Results.NotFound();
        if (section.Revision != request.ExpectedRevision)
            return Results.Conflict(new { code = "song_section_version_conflict", currentRevision = section.Revision });
        section.Name = request.Name.Trim();
        section.Order = request.Order;
        section.Lyrics = request.Lyrics;
        section.Chords = request.Chords;
        section.Revision++;
        section.UpdatedAt = DateTimeOffset.UtcNow;
        section.UpdatedBy = user.Id;
        await db.UpdateAsync(section, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await bus.PublishAsync(new SongSectionsChanged(song.Id, song.BandId));
        return Results.Ok(SongSectionResponse.From(section));
    }
}

public static class DeleteSongSectionEndpoint
{
    [Authorize, Tags("SongSections"), ProducesResponseType(204)]
    [WolverineDelete("/song-api/sections/{sectionId}", OperationId = "DeleteSongSection")]
    public static async Task<IResult> Delete(Guid sectionId, ICurrentUser user,
        [FromServices] DataContext db, [FromServices] IMessageBus bus, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        var section = await db.FindAsync<SongSection>(sectionId, cancellationToken);
        if (section is null) return Results.NotFound();
        var song = await SongAccess.Load(db, section.SongId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, song.BandId, user.Id, true, cancellationToken)) return Results.Forbid();
        if (await db.DeleteAsync<SongSection>(sectionId, cancellationToken) == 0) return Results.NotFound();
        await transaction.CommitAsync(cancellationToken);
        await bus.PublishAsync(new SongSectionsChanged(song.Id, song.BandId));
        return Results.NoContent();
    }
}
