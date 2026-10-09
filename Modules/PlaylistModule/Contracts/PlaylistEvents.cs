namespace WBand.Modules.PlaylistModule.Contracts;

public sealed record PlaylistCreated(Guid PlaylistId, Guid BandId, long ContentVersion);
public sealed record PlaylistChanged(Guid PlaylistId, Guid BandId, long ContentVersion);
public sealed record PlaylistDeleted(Guid PlaylistId, Guid BandId, long ContentVersion);
