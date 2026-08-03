using FluentValidation;
using MongoDB.Bson;
using PlaylistService.Domain;
using PlaylistService.Services;
using Shared;
using Shared.Services;

namespace PlaylistService.Endpoints.Playlists.CreatePlaylist;

/// <summary>
/// Endpoint for creating a new playlist/setlist
/// </summary>
internal sealed class CreatePlaylistEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost("/playlists", Handle)
            .WithName("CreatePlaylist")
            .WithTags("Playlists")
            .RequireAuthorization();
    }

    public static async Task<ApiResponse<Playlist>> Handle(
        CreatePlaylistRequest request,
        IValidator<CreatePlaylistRequest> validator,
        ICurrentUser currentUser,
        Repository repository,
        CancellationToken cancellationToken
    )
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ApiResponse<Playlist>.Error(ApiCodes.ValidationFailed);
        }

        // TODO: Verify user has permission to create playlists in this band

        var playlist = new Playlist
        {
            Id = ObjectId.GenerateNewId().ToString(),
            BandId = request.BandId,
            Name = request.Name,
            Description = request.Description,
            PlannedDate = request.PlannedDate,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.GetUserId,
        };

        if (request.Items != null && request.Items.Count > 0)
        {
            playlist.Items = request
                .Items.Select(
                    (item, index) =>
                        new PlaylistItem
                        {
                            Order = index + 1,
                            Type = item.Type,
                            SongId = item.SongId,
                            CustomKey = item.CustomKey,
                            Notes = item.Notes,
                            DurationMinutes = item.DurationMinutes,
                            BlockTitle = item.BlockTitle,
                        }
                )
                .ToList();

            playlist.DurationMinutes = playlist.Items.Sum(i => i.DurationMinutes);
        }

        await repository.Playlists.InsertOneAsync(playlist, cancellationToken: cancellationToken);

        return ApiResponse<Playlist>.Success(playlist);
    }
}
