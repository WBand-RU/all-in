using WBand.WebAPI.Application.Bands;
using WBand.WebAPI.Data;

namespace WBand.WebAPI.Application.Songs;

internal static class SongAccess
{
    public static async Task<bool> Check(DataContext db, Guid bandId, Guid userId, bool edit, CancellationToken ct)
    {
        var member = await BandAccess.FindMembership(db, bandId, userId, ct);
        return edit ? member?.Role is BandMemberRole.Owner or BandMemberRole.Admin : member is not null;
    }

    // All song writers take the band lock before the song lock, including partition writers.
    // This also serializes edits against membership removal and band deletion.
    public static async Task<Song?> Load(DataContext db, Guid songId, CancellationToken ct, bool includeDeleted = false)
    {
        var song = await db.FindAsync<Song>(songId, ct);
        if (song is null) return null;
        if (db.HasTransaction)
        {
            await db.LockBandAsync(song.BandId, ct);
            song = await db.SingleOrDefaultAsync<Song>("select * from public.songs where id = @songId for update", new { songId }, ct);
        }
        return song is null || (!includeDeleted && song.IsDeleted) ? null : song;
    }

    public static string[] Authors(IReadOnlyList<string>? authors, string? author) =>
        (authors is { Count: > 0 } ? authors : string.IsNullOrWhiteSpace(author) ? [] : [author])
        .Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}
