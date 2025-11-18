namespace SongService.Endpoints.Songs.GetListOfSongs;

public sealed record GetListOfSongsRequest(
    string BandId,
    int Page = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string? SortColumn = null,
    string? SortDirection = null
);
