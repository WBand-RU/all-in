using ChatService.Domain;
using ChatService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared.Services;

namespace ChatService.Hubs;

/// <summary>
/// SignalR hub for real-time chat messaging
/// </summary>
[Authorize]
public sealed class ChatHub : Hub
{
    private readonly Repository repository;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<ChatHub> logger;
    private static readonly Dictionary<string, string> UserConnections = [];

    public ChatHub(Repository repository, ICurrentUser currentUser, ILogger<ChatHub> logger)
    {
        this.repository = repository;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = currentUser.GetUserId;
        var bandId = Context.GetHttpContext()?.Request.Headers["X-Band-Id"].FirstOrDefault();

        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(bandId))
        {
            UserConnections[Context.ConnectionId] = userId;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"band-{bandId}");
            logger.LogInformation("User {UserId} connected to band {BandId} chat", userId, bandId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (UserConnections.ContainsKey(Context.ConnectionId))
        {
            var userId = UserConnections[Context.ConnectionId];
            UserConnections.Remove(Context.ConnectionId);
            logger.LogInformation("User {UserId} disconnected from chat", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Send a message to the band chat
    /// </summary>
    public async Task SendMessage(
        string bandId,
        string content,
        MessageType type = MessageType.Text
    )
    {
        var message = new Message
        {
            Id = ObjectId.GenerateNewId().ToString(),
            BandId = bandId,
            SenderId = currentUser.GetUserId,
            SenderName = currentUser.GetUserEmail ?? "Unknown",
            Content = content,
            Type = type,
            CreatedAt = DateTime.UtcNow,
        };

        await repository.Messages.InsertOneAsync(message);

        await Clients.Group($"band-{bandId}").SendAsync("ReceiveMessage", message);

        logger.LogInformation(
            "Message sent to band {BandId} by user {UserId}",
            bandId,
            currentUser.GetUserId
        );
    }

    /// <summary>
    /// Edit a message
    /// </summary>
    public async Task EditMessage(string messageId, string newContent)
    {
        var message = await repository
            .Messages.Find(m =>
                m.Id == messageId && m.SenderId == currentUser.GetUserId && !m.IsDeleted
            )
            .FirstOrDefaultAsync();

        if (message == null)
        {
            throw new HubException("Message not found or you don't have permission to edit it");
        }

        await repository.Messages.UpdateOneAsync(
            m => m.Id == messageId,
            Builders<Message>
                .Update.Set(m => m.Content, newContent)
                .Set(m => m.IsEdited, true)
                .Set(m => m.EditedAt, DateTime.UtcNow)
        );

        message.Content = newContent;
        message.IsEdited = true;
        message.EditedAt = DateTime.UtcNow;

        await Clients.Group($"band-{message.BandId}").SendAsync("MessageEdited", message);
    }

    /// <summary>
    /// Delete a message
    /// </summary>
    public async Task DeleteMessage(string messageId)
    {
        var message = await repository
            .Messages.Find(m => m.Id == messageId && m.SenderId == currentUser.GetUserId)
            .FirstOrDefaultAsync();

        if (message == null)
        {
            throw new HubException("Message not found or you don't have permission to delete it");
        }

        await repository.Messages.UpdateOneAsync(
            m => m.Id == messageId,
            Builders<Message>.Update.Set(m => m.IsDeleted, true).Set(m => m.Content, "[Deleted]")
        );

        await Clients.Group($"band-{message.BandId}").SendAsync("MessageDeleted", messageId);
    }

    /// <summary>
    /// Add a reaction to a message
    /// </summary>
    public async Task AddReaction(string messageId, string emoji)
    {
        var message = await repository
            .Messages.Find(m => m.Id == messageId && !m.IsDeleted)
            .FirstOrDefaultAsync();

        if (message == null)
        {
            throw new HubException("Message not found");
        }

        var reaction = new MessageReaction
        {
            Emoji = emoji,
            UserId = currentUser.GetUserId,
            UserName = currentUser.GetUserEmail ?? "Unknown",
            CreatedAt = DateTime.UtcNow,
        };

        await repository.Messages.UpdateOneAsync(
            m => m.Id == messageId,
            Builders<Message>.Update.Push(m => m.Reactions, reaction)
        );

        await Clients
            .Group($"band-{message.BandId}")
            .SendAsync("ReactionAdded", messageId, reaction);
    }

    /// <summary>
    /// Start typing indicator
    /// </summary>
    public async Task StartTyping(string bandId)
    {
        await Clients
            .OthersInGroup($"band-{bandId}")
            .SendAsync("UserTyping", currentUser.GetUserId, currentUser.GetUserEmail);
    }

    /// <summary>
    /// Stop typing indicator
    /// </summary>
    public async Task StopTyping(string bandId)
    {
        await Clients
            .OthersInGroup($"band-{bandId}")
            .SendAsync("UserStoppedTyping", currentUser.GetUserId);
    }
}
