namespace SongService.Endpoints.Songs.CreateSong;

public sealed record CreateSongRequest(
    string BandId,
    string Title,
    string? Author,
    string? Lyrics,
    string? Chords,
    string? Key,
    int? Bpm
);
