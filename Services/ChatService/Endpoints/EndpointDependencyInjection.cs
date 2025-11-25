using ChatService.Endpoints.Messages.GetMessages;

namespace ChatService.Endpoints;

/// <summary>
/// Extension methods for endpoint dependency injection
/// </summary>
public static class EndpointDependencyInjection
{
    public static IEndpointRouteBuilder AddEndpoints(this IEndpointRouteBuilder endpoints)
    {
        GetMessagesEndpoint.Build(endpoints);

        return endpoints;
    }
}
