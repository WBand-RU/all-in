using FluentValidation;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Shared.Services;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.SongModule.Contracts;
using WBand.Modules.SongModule.Domain;
using Wolverine;
using Wolverine.Http;

namespace WBand.Modules.SongModule.Endpoints;

public sealed record CreateSongSectionRequest(string Name, int Order, int BarCount, string? Lyrics, string? Chords);
public sealed record UpdateSongSectionRequest(string Name, int Order, int BarCount, string? Lyrics, string? Chords,
    long ExpectedRevision);
public sealed record SongSectionResponse(Guid Id, Guid SongId, string Name, int Order, int BarCount, string? Lyrics,
    string? Chords, long Revision, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt)
{
    public static SongSectionResponse From(SongSection section) => new(section.Id, section.SongId,
        section.Name, section.Order, section.BarCount, section.Lyrics, section.Chords, section.Revision,
        section.CreatedAt, section.UpdatedAt);
}

public sealed class CreateSongSectionRequestValidator : AbstractValidator<CreateSongSectionRequest>
{
    public CreateSongSectionRequestValidator() => Configure(this, request => request.Name,
        request => request.Order, request => request.BarCount, request => request.Lyrics, request => request.Chords);

    internal static void Configure<T>(AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, string>> name,
        System.Linq.Expressions.Expression<Func<T, int>> order,
        System.Linq.Expressions.Expression<Func<T, int>> barCount,
        System.Linq.Expressions.Expression<Func<T, string?>> lyrics,
        System.Linq.Expressions.Expression<Func<T, string?>> chords)
    {
        validator.RuleFor(name).NotEmpty().MaximumLength(100);
        validator.RuleFor(order).GreaterThanOrEqualTo(0);
        validator.RuleFor(barCount).InclusiveBetween(1, 10_000);
        validator.RuleFor(lyrics).MaximumLength(50_000);
        validator.RuleFor(chords).MaximumLength(50_000);
    }
}

public sealed class UpdateSongSectionRequestValidator : AbstractValidator<UpdateSongSectionRequest>
{
    public UpdateSongSectionRequestValidator()
    {
        CreateSongSectionRequestValidator.Configure(this, request => request.Name,
            request => request.Order, request => request.BarCount, request => request.Lyrics, request => request.Chords);
        RuleFor(request => request.ExpectedRevision).GreaterThan(0);
    }
}

internal static class SongSectionSupport
{
    public static async Task<Song?> LoadSong(Guid songId, IQuerySession session,
        CancellationToken cancellationToken) =>
        await session.LoadAsync<Song>(songId, cancellationToken) is { IsDeleted: false } song
            ? song
            : null;
}

public static class ListSongSectionsEndpoint
{
    [Authorize]
    [WolverineGet("/song-api/songs/{songId}/sections")]
    public static async Task<IResult> Get(Guid songId, ICurrentUser user, IQuerySession session,
        IMessageBus bus, CancellationToken cancellationToken)
    {
        var song = await SongSectionSupport.LoadSong(songId, session, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(bus, song.BandId, user.GetUserId, BandPermission.View))
            return Results.Forbid();
        var sections = await session.Query<SongSection>().Where(section => section.SongId == songId)
            .OrderBy(section => section.Order).ThenBy(section => section.CreatedAt)
            .ToListAsync(cancellationToken);
        return Results.Ok(sections.Select(SongSectionResponse.From).ToArray());
    }
}

public static class CreateSongSectionEndpoint
{
    [Authorize]
    [WolverinePost("/song-api/songs/{songId}/sections")]
    public static async Task<IResult> Post(Guid songId, CreateSongSectionRequest request,
        ICurrentUser user, IDocumentSession session, IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var song = await SongSectionSupport.LoadSong(songId, session, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(bus, song.BandId, user.GetUserId, BandPermission.EditContent))
            return Results.Forbid();
        var section = new SongSection { Id = Guid.CreateVersion7(), SongId = songId,
            BandId = song.BandId, Name = request.Name.Trim(), Order = request.Order,
            BarCount = request.BarCount,
            Lyrics = EmptyToNull(request.Lyrics), Chords = EmptyToNull(request.Chords),
            CreatedAt = DateTimeOffset.UtcNow, CreatedBy = user.GetUserId };
        session.Store(section);
        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new SongSectionsChanged(songId, song.BandId));
        return Results.Ok(SongSectionResponse.From(section));
    }

    internal static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.TrimEnd();
}

public static class UpdateSongSectionEndpoint
{
    [Authorize]
    [WolverinePut("/song-api/sections/{sectionId}")]
    public static async Task<IResult> Put(Guid sectionId, UpdateSongSectionRequest request,
        ICurrentUser user, IDocumentSession session, IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var section = await session.LoadAsync<SongSection>(sectionId, cancellationToken);
        if (section is null) return Results.NotFound();
        if (!await SongAccess.Check(bus, section.BandId, user.GetUserId, BandPermission.EditContent))
            return Results.Forbid();
        if (section.Revision != request.ExpectedRevision)
            return Results.Conflict(new { code = "song_section_version_conflict",
                currentRevision = section.Revision });
        section.Name = request.Name.Trim(); section.Order = request.Order;
        section.BarCount = request.BarCount;
        section.Lyrics = CreateSongSectionEndpoint.EmptyToNull(request.Lyrics);
        section.Chords = CreateSongSectionEndpoint.EmptyToNull(request.Chords);
        section.Revision++; section.UpdatedAt = DateTimeOffset.UtcNow;
        section.UpdatedBy = user.GetUserId;
        session.Store(section);
        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new SongSectionsChanged(section.SongId, section.BandId));
        return Results.Ok(SongSectionResponse.From(section));
    }
}

public static class DeleteSongSectionEndpoint
{
    [Authorize]
    [WolverineDelete("/song-api/sections/{sectionId}")]
    public static async Task<IResult> Delete(Guid sectionId, ICurrentUser user,
        IDocumentSession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        var section = await session.LoadAsync<SongSection>(sectionId, cancellationToken);
        if (section is null) return Results.NotFound();
        if (!await SongAccess.Check(bus, section.BandId, user.GetUserId, BandPermission.EditContent))
            return Results.Forbid();
        session.Delete(section);
        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new SongSectionsChanged(section.SongId, section.BandId));
        return Results.NoContent();
    }
}
