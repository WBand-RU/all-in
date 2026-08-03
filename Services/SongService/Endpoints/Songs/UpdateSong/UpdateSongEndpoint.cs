using FluentValidation;
using MongoDB.Driver;
using Shared;
using Shared.Services;
using SongService.Domain;
using SongService.Services;

namespace SongService.Endpoints.Songs.UpdateSong;

/// <summary>
/// Endpoint for updating an existing song
/// </summary>
internal sealed class UpdateSongEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/{songId}", Handle).WithName("UpdateSong");
    }

    public static async Task<ApiResponse<Song>> Handle(
        string songId,
        UpdateSongRequest request,
        IValidator<UpdateSongRequest> validator,
        ICurrentUser currentUser,
        Repository repository,
        CancellationToken cancellationToken
    )
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ApiResponse<Song>.Error(ApiCodes.ValidationFailed);
        }

        var existingSong = await repository
            .Songs.Find(s => s.Id == songId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingSong == null)
        {
            return ApiResponse<Song>.Error(ApiCodes.NotFound);
        }

        // TODO: Verify user has permission to edit songs in this band
        // This will be implemented with the permission system

        var updateDefinition = Builders<Song>
            .Update.Set(s => s.Title, request.Title)
            .Set(s => s.Author, request.Author)
            .Set(s => s.Lyrics, request.Lyrics)
            .Set(s => s.Chords, request.Chords)
            .Set(s => s.Key, request.Key)
            .Set(s => s.Bpm, request.Bpm)
            .Set(s => s.UpdatedAt, DateTime.UtcNow)
            .Set(s => s.UpdatedBy, currentUser.GetUserId);

        var updatedSong = await repository.Songs.FindOneAndUpdateAsync(
            s => s.Id == songId,
            updateDefinition,
            new FindOneAndUpdateOptions<Song> { ReturnDocument = ReturnDocument.After },
            cancellationToken
        );

        return ApiResponse<Song>.Success(updatedSong);
    }
}
