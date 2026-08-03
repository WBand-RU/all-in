using ChatService.Domain;
using ChatService.Services;
using MongoDB.Driver;
using Shared;
using Shared.Services;

namespace ChatService.Endpoints.Messages.GetMessages;

/// <summary>
/// Endpoint to get message history for a band
/// </summary>
internal sealed class GetMessagesEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost("/messages/list", Handle)
            .WithName("GetMessages")
            .WithTags("Messages")
            .RequireAuthorization();
    }

    public static async Task<ApiResponse<TableResponse<Message>>> Handle(
        GetMessagesRequest request,
        ICurrentUser currentUser,
        Repository repository,
        CancellationToken cancellationToken
    )
    {
        // TODO: Check if user is a member of the band
        // For now, we'll assume they have access if authenticated

        var filter = Builders<Message>.Filter.And(
            Builders<Message>.Filter.Eq(m => m.BandId, request.BandId),
            Builders<Message>.Filter.Eq(m => m.IsDeleted, false)
        );

        var totalCount = await repository.Messages.CountDocumentsAsync(
            filter,
            cancellationToken: cancellationToken
        );

        var messages = await repository
            .Messages.Find(filter)
            .SortByDescending(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Limit(request.PageSize)
            .ToListAsync(cancellationToken);

        // Reverse to get chronological order
        messages.Reverse();

        return ApiResponse<TableResponse<Message>>.Success(
            new TableResponse<Message>(totalCount, messages)
        );
    }
}
