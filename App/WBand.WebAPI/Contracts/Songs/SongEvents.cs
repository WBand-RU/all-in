namespace WBand.Modules.SongModule.Contracts;

/// <summary>A song integration contract handled through Wolverine.</summary>
public sealed record SongCreated(Guid SongId, Guid BandId, long ContentVersion);
/// <summary>A song integration contract handled through Wolverine.</summary>
public sealed record SongChanged(Guid SongId, Guid BandId, long ContentVersion);
/// <summary>A song integration contract handled through Wolverine.</summary>
public sealed record SongDeleted(Guid SongId, Guid BandId, long ContentVersion);
/// <summary>A song integration contract handled through Wolverine.</summary>
public sealed record SongRestored(Guid SongId, Guid BandId, long ContentVersion);
/// <summary>A song integration contract handled through Wolverine.</summary>
public sealed record SongSectionsChanged(Guid SongId, Guid BandId);

/// <summary>A song integration contract handled through Wolverine.</summary>
public sealed record GetSongReferences(IReadOnlyList<Guid> SongIds);
/// <summary>A song integration contract handled through Wolverine.</summary>
public sealed record SongReference(Guid SongId, Guid BandId, string Title, long ContentVersion);
