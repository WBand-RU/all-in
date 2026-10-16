using Microsoft.EntityFrameworkCore;
using WBand.Modules.SongModule.Contracts;
using WBand.WebAPI.Data;

namespace WBand.WebAPI.Application.Songs;

/// <summary>Resolves current song references for Wolverine consumers.</summary>
public static class GetSongReferencesHandler
{
    public static async Task<SongReference[]> Handle(GetSongReferences query, DataContext db, CancellationToken cancellationToken)
    {
        var ids = query.SongIds.Distinct().ToArray();
        if (ids.Length == 0) return [];
        var songs = await db.Songs.AsNoTracking()
            .Where(song => ids.Contains(song.Id) && song.DeletedAt == null)
            .ToListAsync(cancellationToken);
        return songs.Select(song => new SongReference(song.Id, song.BandId, song.Title, song.ContentVersion)).ToArray();
    }
}
