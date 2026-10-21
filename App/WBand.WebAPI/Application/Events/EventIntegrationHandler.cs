using WBand.Modules.BandModule.Contracts;
using WBand.Modules.EventModule.Contracts;
using WBand.Modules.SongModule.Contracts;
using Wolverine;

namespace WBand.WebAPI.Application.Events;

/// <summary>Adapts event queries to the owning band and song module contracts.</summary>
public static class EventIntegrationHandler
{
    public static Task<bool> Handle(CheckEventBandAccess query, IMessageBus bus, CancellationToken ct) =>
        bus.InvokeAsync<bool>(new CheckBandPermission(query.BandId, query.UserId,
            query.Edit ? BandPermission.EditContent : BandPermission.View), ct);
    public static Task<Guid[]> Handle(GetEventUserBands query, IMessageBus bus, CancellationToken ct) =>
        bus.InvokeAsync<Guid[]>(new GetUserBandIds(query.UserId), ct);
    public static async Task<EventSongReference[]> Handle(ResolveEventSongs query, IMessageBus bus, CancellationToken ct) =>
        (await bus.InvokeAsync<SongReference[]>(new GetSongReferences(query.SongIds), ct))
        .Select(x => new EventSongReference(x.SongId, x.BandId)).ToArray();
}
