using WBand.Modules.SongModule.Domain;

namespace WBand.Modules.SongModule.Application;

public static class SongVersioning
{
    public static SongSnapshot Snapshot(Song song) => new(
        song.Title, [.. song.Authors], song.Key, song.Bpm, [.. song.TempoTrack],
        [.. song.TimeSignatureTrack], song.CountInBars, [.. song.Sections],
        song.Lyrics, song.Chords, song.Status);

    public static SongRevision Revision(Song song, Guid userId, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(), SongId = song.Id, ContentVersion = song.ContentVersion,
        Snapshot = Snapshot(song), CreatedAt = now, CreatedBy = userId,
    };

    public static void Apply(Song song, SongSnapshot value)
    {
        song.Title = value.Title;
        song.Authors = [.. value.Authors];
        song.Key = value.Key;
        song.Bpm = value.Bpm;
        song.TempoTrack = [.. value.TempoTrack];
        song.TimeSignatureTrack = [.. value.TimeSignatureTrack];
        song.CountInBars = value.CountInBars;
        song.Sections = [.. value.Sections];
        song.Lyrics = value.Lyrics;
        song.Chords = value.Chords;
        song.Status = value.Status;
    }
}
