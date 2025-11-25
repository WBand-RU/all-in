using MongoDB.Driver;
using PlaylistService.Domain;

namespace PlaylistService.Services;

public sealed class Repository(IMongoDatabase database)
{
    public IMongoCollection<Playlist> Playlists => database.GetCollection<Playlist>("playlists");

    public Task<IClientSessionHandle> StartSessionAsync() => database.Client.StartSessionAsync();
}
