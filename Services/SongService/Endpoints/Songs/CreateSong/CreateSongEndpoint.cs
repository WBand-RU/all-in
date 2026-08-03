using FluentValidation;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared;
using Shared.Services;
using SongService.Domain;
using SongService.Services;

namespace SongService.Endpoints.Songs.CreateSong;

/// <summary>
/// Endpoint for creating a new song
/// </summary>
internal sealed class CreateSongEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("", Handle).WithName("CreateSong");
    }

    public static async Task<ApiResponse<Song>> Handle(
        CreateSongRequest request,
        IValidator<CreateSongRequest> validator,
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

        // TODO: Verify user has permission to add songs to this band
        // This will be implemented with the permission system

        var song = new Song
        {
            Id = ObjectId.GenerateNewId().ToString(),
            BandId = request.BandId,
            Title = request.Title,
            Author = request.Author,
            Lyrics = request.Lyrics,
            Chords = request.Chords,
            Key = request.Key,
            Bpm = request.Bpm,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.GetUserId,
        };

        await repository.Songs.InsertOneAsync(song, cancellationToken: cancellationToken);

        return ApiResponse<Song>.Success(song);
    }
}
