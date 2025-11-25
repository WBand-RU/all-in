using MongoDB.Driver;
using Shared;
using SongService.Domain;

namespace SongService.Services;

public sealed class Repository(IMongoDatabase database)
{
    public IMongoCollection<Song> Songs => database.GetCollection<Song>("songs");

    public Task<IClientSessionHandle> StartSessionAsync() => database.Client.StartSessionAsync();
}
