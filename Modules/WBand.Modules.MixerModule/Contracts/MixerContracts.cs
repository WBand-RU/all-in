namespace WBand.Modules.MixerModule.Contracts;

public sealed record GenerateSongMixes(Guid BatchId, Guid SongId, Guid BandId, Guid RequestedBy);
