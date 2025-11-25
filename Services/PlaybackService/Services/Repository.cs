using MongoDB.Driver;
using PlaybackService.Domain;

namespace PlaybackService.Services;

public sealed class Repository(IMongoDatabase database)
{
    public IMongoCollection<Track> Tracks => database.GetCollection<Track>("tracks");

    public IMongoCollection<MixerPreset> MixerPresets =>
        database.GetCollection<MixerPreset>("mixer_presets");

    public Task<IClientSessionHandle> StartSessionAsync() => database.Client.StartSessionAsync();
}
