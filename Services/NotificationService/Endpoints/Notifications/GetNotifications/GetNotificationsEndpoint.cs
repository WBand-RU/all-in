using MongoDB.Driver;
using NotificationService.Domain;
using NotificationService.Services;
using Shared;
using Shared.Services;

namespace NotificationService.Endpoints.Notifications.GetNotifications;

/// <summary>
/// Endpoint to get notifications for the current user
/// </summary>
internal sealed class GetNotificationsEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/notifications", Handle)
            .WithName("GetNotifications")
            .WithTags("Notifications")
            .RequireAuthorization();
    }

    public static async Task<ApiResponse<List<Notification>>> Handle(
        ICurrentUser currentUser,
        Repository repository,
        bool? unreadOnly,
        CancellationToken cancellationToken
    )
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.UserId, currentUser.GetUserId);

        if (unreadOnly == true)
        {
            filter = Builders<Notification>.Filter.And(
                filter,
                Builders<Notification>.Filter.Eq(n => n.IsRead, false)
            );
        }

        var notifications = await repository
            .Notifications.Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .Limit(100)
            .ToListAsync(cancellationToken);

        return ApiResponse<List<Notification>>.Success(notifications);
    }
}
