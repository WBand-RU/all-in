namespace WBand.Modules.EventModule.Contracts;

/// <summary>Checks band membership or content editing permission through the owning module.</summary>
public sealed record CheckEventBandAccess(Guid BandId, Guid UserId, bool Edit);
/// <summary>Lists bands available to an authenticated user.</summary>
public sealed record GetEventUserBands(Guid UserId);
/// <summary>Resolves active songs through the song module before assigning an event setlist.</summary>
public sealed record ResolveEventSongs(Guid[] SongIds);
/// <summary>Identifies an active song and its owning band.</summary>
public sealed record EventSongReference(Guid SongId, Guid BandId);
