using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using NotificationService.Domain;
using NotificationService.Services;
using Shared.Services;

namespace NotificationService.Hubs;

/// <summary>
/// SignalR hub for real-time notification delivery
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    private readonly Repository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<NotificationHub> _logger;
    private static readonly Dictionary<string, List<string>> _userConnections = new();

    public NotificationHub(
        Repository repository,
        ICurrentUser currentUser,
        ILogger<NotificationHub> logger
    )
    {
        _repository = repository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = _currentUser.GetUserId;

        if (!string.IsNullOrEmpty(userId))
        {
            lock (_userConnections)
            {
                if (!_userConnections.ContainsKey(userId))
                {
                    _userConnections[userId] = new List<string>();
                }
                _userConnections[userId].Add(Context.ConnectionId);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

            // Send unread notification count on connect
            var unreadCount = await _repository.Notifications.CountDocumentsAsync(n =>
                n.UserId == userId && !n.IsRead
            );

            await Clients.Caller.SendAsync("UnreadCount", unreadCount);

            _logger.LogInformation("User {UserId} connected to notifications", userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _currentUser.GetUserId;

        if (!string.IsNullOrEmpty(userId))
        {
            lock (_userConnections)
            {
                if (_userConnections.ContainsKey(userId))
                {
                    _userConnections[userId].Remove(Context.ConnectionId);
                    if (_userConnections[userId].Count == 0)
                    {
                        _userConnections.Remove(userId);
                    }
                }
            }

            _logger.LogInformation("User {UserId} disconnected from notifications", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    public async Task MarkAsRead(string notificationId)
    {
        var notification = await _repository
            .Notifications.Find(n => n.Id == notificationId && n.UserId == _currentUser.GetUserId)
            .FirstOrDefaultAsync();

        if (notification == null)
        {
            throw new HubException("Notification not found");
        }

        await _repository.Notifications.UpdateOneAsync(
            n => n.Id == notificationId,
            Builders<Notification>
                .Update.Set(n => n.IsRead, true)
                .Set(n => n.ReadAt, DateTime.UtcNow)
        );

        // Update unread count
        var unreadCount = await _repository.Notifications.CountDocumentsAsync(n =>
            n.UserId == _currentUser.GetUserId && !n.IsRead
        );

        await Clients.Caller.SendAsync("UnreadCount", unreadCount);
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    public async Task MarkAllAsRead()
    {
        await _repository.Notifications.UpdateManyAsync(
            n => n.UserId == _currentUser.GetUserId && !n.IsRead,
            Builders<Notification>
                .Update.Set(n => n.IsRead, true)
                .Set(n => n.ReadAt, DateTime.UtcNow)
        );

        await Clients.Caller.SendAsync("UnreadCount", 0);
    }

    /// <summary>
    /// Send a notification to a specific user (internal method to be called from services)
    /// </summary>
    public static async Task SendNotificationToUser(
        IHubContext<NotificationHub> hubContext,
        string userId,
        Notification notification
    )
    {
        await hubContext
            .Clients.Group($"user-{userId}")
            .SendAsync("ReceiveNotification", notification);
    }

    /// <summary>
    /// Send a notification to all band members (internal method to be called from services)
    /// </summary>
    public static async Task SendNotificationToBand(
        IHubContext<NotificationHub> hubContext,
        Repository repository,
        string bandId,
        string title,
        string content,
        NotificationType type,
        NotificationSeverity severity = NotificationSeverity.Info
    )
    {
        // TODO: Get band members from BandService
        // For now, this is a placeholder implementation

        var notification = new Notification
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserId = "placeholder", // Will be set per user
            BandId = bandId,
            Title = title,
            Content = content,
            Type = type,
            Severity = severity,
            CreatedAt = DateTime.UtcNow,
        };

        // In a real implementation, we would:
        // 1. Get all band members from BandService
        // 2. Create a notification for each member
        // 3. Save to database
        // 4. Send via SignalR to each member's group

        await Task.CompletedTask;
    }
}
