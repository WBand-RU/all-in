namespace WBand.Modules.SongModule.Contracts;

public sealed record SongCreated(Guid SongId, Guid BandId, long ContentVersion);
public sealed record SongChanged(Guid SongId, Guid BandId, long ContentVersion);
public sealed record SongDeleted(Guid SongId, Guid BandId, long ContentVersion);
public sealed record SongRestored(Guid SongId, Guid BandId, long ContentVersion);

public sealed record GetSongReferences(IReadOnlyList<Guid> SongIds);
public sealed record SongReference(Guid SongId, Guid BandId, string Title, long ContentVersion);
