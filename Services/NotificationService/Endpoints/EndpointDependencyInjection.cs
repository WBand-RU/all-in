using NotificationService.Endpoints.Notifications.GetNotifications;

namespace NotificationService.Endpoints;

/// <summary>
/// Extension methods for endpoint dependency injection
/// </summary>
public static class EndpointDependencyInjection
{
    public static IEndpointRouteBuilder AddEndpoints(this IEndpointRouteBuilder endpoints)
    {
        GetNotificationsEndpoint.Build(endpoints);

        return endpoints;
    }
}
