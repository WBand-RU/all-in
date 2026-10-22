using WBand.Persistence;

namespace WBand.WebAPI.Application.Songs;

internal static class SongVersioning
{
    public static Task SaveRevision(RelationalDataContext db, Song song, Guid userId, DateTimeOffset now, CancellationToken ct) =>
        db.InsertAsync(new SongRevision
        {
            Id = Guid.CreateVersion7(), SongId = song.Id, ContentVersion = song.ContentVersion,
            Title = song.Title, Authors = [.. song.Authors], Key = song.Key, Bpm = song.Bpm,
            Status = song.Status, CreatedAt = now, CreatedBy = userId,
        }, ct);

    public static void Apply(Song song, SongRevision revision)
    {
        song.Title = revision.Title;
        song.Authors = [.. revision.Authors];
        song.Key = revision.Key;
        song.Bpm = revision.Bpm;
        song.Status = revision.Status;
    }

    public static void Changed(Song song, Guid userId)
    {
        song.ContentVersion++;
        song.UpdatedAt = DateTimeOffset.UtcNow;
        song.UpdatedBy = userId;
    }
}
