using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Shared;
using Shared.Services;
using SongService.Domain;
using SongService.Services;

namespace SongService.Endpoints.Songs.GetListOfSongs;

/// <summary>
/// Endpoint for retrieving a paginated list of songs
/// </summary>
internal sealed class GetListOfSongsEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("", Handle).WithName("GetListOfSongs");
    }

    public static async Task<ApiResponse<TableResponse<Song>>> Handle(
        [FromQuery] string bandId,
        ICurrentUser currentUser,
        Repository repository,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortColumn = null,
        [FromQuery] string? sortDirection = null
    )
    {
        // TODO: Verify user has permission to view songs in this band
        // This will be implemented with the permission system

        var filter = Builders<Song>.Filter.Eq(s => s.BandId, bandId);

        // Add search filter if provided
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchFilter = Builders<Song>.Filter.Or(
                Builders<Song>.Filter.Regex(
                    s => s.Title,
                    new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")
                ),
                Builders<Song>.Filter.Regex(
                    s => s.Author,
                    new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")
                )
            );
            filter = Builders<Song>.Filter.And(filter, searchFilter);
        }

        var totalCount = await repository.Songs.CountDocumentsAsync(
            filter,
            cancellationToken: cancellationToken
        );

        var sortDefinition =
            sortDirection?.ToLower() == "desc"
                ? Builders<Song>.Sort.Descending(sortColumn ?? "title")
                : Builders<Song>.Sort.Ascending(sortColumn ?? "title");

        var songs = await repository
            .Songs.Find(filter)
            .Sort(sortDefinition)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        var response = new TableResponse<Song>(totalCount, songs);

        return ApiResponse<TableResponse<Song>>.Success(response);
    }
}
