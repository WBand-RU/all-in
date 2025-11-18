using ChatService.Domain;
using MongoDB.Driver;

namespace ChatService.Services;

/// <summary>
/// Repository for chat data access
/// </summary>
public sealed class Repository
{
    private readonly IMongoDatabase _database;

    public Repository(IMongoClient mongoClient)
    {
        _database = mongoClient.GetDatabase("chats");
    }

    public IMongoCollection<Message> Messages => _database.GetCollection<Message>("messages");

    public IClientSessionHandle? Session { get; set; }
}
