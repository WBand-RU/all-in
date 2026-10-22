using Wolverine;
using WBand.Modules.BandModule.Contracts;
using DataContext = WBand.Modules.SongModule.Data.SongDataContext;

namespace WBand.WebAPI.Application.Songs;

internal static class SongAccess
{
    public static async Task<bool> Check(DataContext db, IMessageBus bus, Guid bandId, Guid userId, bool edit, CancellationToken ct)
    {
        if (db.HasTransaction) await db.LockBandAsync(bandId, ct);
        return await bus.InvokeAsync<bool>(new CheckBandPermission(bandId, userId,
            edit ? BandPermission.EditContent : BandPermission.View), ct);
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
            song = await db.SingleOrDefaultAsync<Song>("select * from songs where id = @songId for update", new { songId }, ct);
        }
        return song is null || (!includeDeleted && song.IsDeleted) ? null : song;
    }

    public static string[] Authors(IReadOnlyList<string>? authors, string? author) =>
        (authors is { Count: > 0 } ? authors : string.IsNullOrWhiteSpace(author) ? [] : [author])
        .Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}
