using MongoDB.Driver;
using NotificationService.Domain;

namespace NotificationService.Services;

/// <summary>
/// Repository for notification data access
/// </summary>
public sealed class Repository
{
    private readonly IMongoDatabase _database;

    public Repository(IMongoClient mongoClient)
    {
        _database = mongoClient.GetDatabase("notifications");
    }

    public IMongoCollection<Notification> Notifications =>
        _database.GetCollection<Notification>("notifications");

    public IClientSessionHandle? Session { get; set; }
}
