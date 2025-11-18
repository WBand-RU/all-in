namespace SongService.Endpoints.Songs.UpdateSong;

public sealed record UpdateSongRequest(
    string Title,
    string? Author,
    string? Lyrics,
    string? Chords,
    string? Key,
    int? Bpm
);
