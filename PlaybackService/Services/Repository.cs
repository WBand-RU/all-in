using MongoDB.Driver;
using PlaybackService.Domain;

namespace PlaybackService.Services;

public sealed class Repository(IMongoDatabase database)
{
    public IMongoCollection<Playback> Playbacks => database.GetCollection<Playback>("playbacks");

    public Task<IClientSessionHandle> StartSessionAsync() => database.Client.StartSessionAsync();
}
