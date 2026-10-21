using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WBand.Modules.EventModule.Contracts;
using WBand.Modules.EventModule.Data;
using Wolverine;
using Wolverine.Http;

namespace WBand.Modules.EventModule.Endpoints;

/// <summary>Event metadata and ordered song identifiers. Version protects concurrent edits.</summary>
public sealed record SaveEventRequest(Guid BandId, string Title, DateTimeOffset StartsAt,
    string? Venue, string? Description, Guid[] SongIds, long? ExpectedVersion);
/// <summary>An event returned to members of its band.</summary>
public sealed record EventResponse(Guid Id, Guid BandId, string Title, DateTimeOffset StartsAt,
    string? Venue, string? Description, Guid[] SongIds, long Version)
{
    internal static EventResponse From(PerformanceEvent item) =>
        new(item.Id, item.BandId, item.Title, item.StartsAt, item.Venue, item.Description, item.SongIds, item.Version);
}

/// <summary>Authenticated event endpoints. Cross-module lookups use Wolverine messages.</summary>
public static class EventEndpoints
{
    private static Guid Subject(HttpContext context) => Guid.Parse(
        context.User.FindFirstValue("sub") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [Authorize, Tags("Events"), ProducesResponseType<EventResponse[]>(200)]
    [WolverineGet("/api/events", OperationId = "ListEvents")]
    public static async Task<IResult> List(HttpContext context, [FromServices] EventDataContext db,
        IMessageBus bus, CancellationToken ct)
    {
        var bands = await bus.InvokeAsync<Guid[]>(new GetEventUserBands(Subject(context)), ct);
        var events = await db.Events.AsNoTracking().Where(x => bands.Contains(x.BandId))
            .OrderBy(x => x.StartsAt).ThenBy(x => x.Id).ToListAsync(ct);
        return Results.Ok(events.Select(EventResponse.From).ToArray());
    }

    [Authorize, Tags("Events"), ProducesResponseType<EventResponse>(200)]
    [WolverineGet("/api/events/{eventId}", OperationId = "GetEvent")]
    public static async Task<IResult> Get(Guid eventId, HttpContext context, [FromServices] EventDataContext db,
        IMessageBus bus, CancellationToken ct)
    {
        var item = await db.Events.AsNoTracking().SingleOrDefaultAsync(x => x.Id == eventId, ct);
        if (item is null) return Results.NotFound();
        if (!await bus.InvokeAsync<bool>(new CheckEventBandAccess(item.BandId, Subject(context), false), ct)) return Results.Forbid();
        return Results.Ok(EventResponse.From(item));
    }

    [Authorize, Tags("Events"), ProducesResponseType<EventResponse>(200)]
    [WolverinePost("/api/events", OperationId = "CreateEvent")]
    public static async Task<IResult> Create(SaveEventRequest request, HttpContext context,
        [FromServices] EventDataContext db, IMessageBus bus, CancellationToken ct)
    {
        var error = await Validate(request, Subject(context), bus, ct);
        if (error is not null) return error;
        var item = new PerformanceEvent { BandId = request.BandId, Title = request.Title.Trim(),
            StartsAt = request.StartsAt.ToUniversalTime(), Venue = request.Venue?.Trim(), Description = request.Description?.Trim(),
            SongIds = request.SongIds };
        db.Events.Add(item);
        await db.SaveChangesAsync(ct);
        return Results.Ok(EventResponse.From(item));
    }

    [Authorize, Tags("Events"), ProducesResponseType<EventResponse>(200)]
    [WolverinePut("/api/events/{eventId}", OperationId = "UpdateEvent")]
    public static async Task<IResult> Update(Guid eventId, SaveEventRequest request, HttpContext context,
        [FromServices] EventDataContext db, IMessageBus bus, CancellationToken ct)
    {
        var item = await db.Events.SingleOrDefaultAsync(x => x.Id == eventId, ct);
        if (item is null) return Results.NotFound();
        if (!await bus.InvokeAsync<bool>(new CheckEventBandAccess(item.BandId, Subject(context), true), ct)) return Results.Forbid();
        if (request.BandId != item.BandId) return Results.BadRequest(new { message = "Нельзя изменить группу события." });
        var error = await Validate(request, Subject(context), bus, ct);
        if (error is not null) return error;
        if (request.ExpectedVersion != item.Version) return Results.Conflict(new { message = "Событие изменено другим участником. Обновите страницу." });
        item.Title = request.Title.Trim(); item.StartsAt = request.StartsAt.ToUniversalTime();
        item.Venue = request.Venue?.Trim(); item.Description = request.Description?.Trim(); item.SongIds = request.SongIds;
        item.Version++;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Results.Conflict(); }
        return Results.Ok(EventResponse.From(item));
    }

    [Authorize, Tags("Events")]
    [WolverineDelete("/api/events/{eventId}", OperationId = "DeleteEvent")]
    public static async Task<IResult> Delete(Guid eventId, HttpContext context,
        [FromServices] EventDataContext db, IMessageBus bus, CancellationToken ct)
    {
        var item = await db.Events.SingleOrDefaultAsync(x => x.Id == eventId, ct);
        if (item is null) return Results.NotFound();
        if (!await bus.InvokeAsync<bool>(new CheckEventBandAccess(item.BandId, Subject(context), true), ct)) return Results.Forbid();
        db.Events.Remove(item);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Results.Conflict(); }
        return Results.NoContent();
    }

    private static async Task<IResult?> Validate(SaveEventRequest request, Guid userId, IMessageBus bus, CancellationToken ct)
    {
        if (!await bus.InvokeAsync<bool>(new CheckEventBandAccess(request.BandId, userId, true), ct)) return Results.Forbid();
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200 || request.StartsAt == default
            || request.Venue?.Length > 500 || request.Description?.Length > 5000 || request.SongIds is null
            || request.SongIds.Length > 200 || request.SongIds.Distinct().Count() != request.SongIds.Length)
            return Results.BadRequest(new { message = "Проверьте название, дату и список песен (до 200 без повторов)." });
        var songs = await bus.InvokeAsync<EventSongReference[]>(new ResolveEventSongs(request.SongIds), ct);
        if (songs.Length != request.SongIds.Length || songs.Any(x => x.BandId != request.BandId))
            return Results.BadRequest(new { message = "Можно добавлять только доступные песни выбранной группы." });
        return null;
    }
}
