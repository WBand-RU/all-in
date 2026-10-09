namespace WBand.Modules.StemModule.Contracts;

public sealed record StemsChanged(Guid SongId, Guid BandId, long ContentVersion);

public sealed record GetReadyStemsForMix(Guid SongId);
public sealed record ReadyStemForMix(Guid StemId, Guid SongId, Guid BandId, Guid FileId,
    string Name, string Kind);

/// <summary>Signals that a derived playback plan or artifact must be rebuilt.</summary>
public sealed record PlaybackCacheInvalidated(
    Guid BandId,
    string ResourceType,
    Guid ResourceId,
    long ContentVersion,
    string Reason);
