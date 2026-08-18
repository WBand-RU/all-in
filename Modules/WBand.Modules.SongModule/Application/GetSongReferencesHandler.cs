using Marten;
using WBand.Modules.SongModule.Contracts;
using WBand.Modules.SongModule.Domain;

namespace WBand.Modules.SongModule.Application;

public static class GetSongReferencesHandler
{
    public static async Task<SongReference[]> Handle(
        GetSongReferences query,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        var ids = query.SongIds.Distinct().ToArray();
        if (ids.Length == 0) return [];

        var songs = await session.Query<Song>()
            .Where(song => ids.Contains(song.Id) && song.DeletedAt == null)
            .ToListAsync(cancellationToken);

        return songs.Select(song => new SongReference(
            song.Id, song.BandId, song.Title, song.ContentVersion)).ToArray();
    }
}
