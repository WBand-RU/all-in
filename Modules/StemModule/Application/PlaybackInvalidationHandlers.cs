using WBand.Modules.PlaylistModule.Contracts;
using WBand.Modules.SongModule.Contracts;
using WBand.Modules.StemModule.Contracts;

namespace WBand.Modules.StemModule.Application;

public static class PlaybackInvalidationHandlers
{
    public static PlaybackCacheInvalidated Handle(SongChanged message) =>
        new(message.BandId, "song", message.SongId, message.ContentVersion, "song_changed");

    public static PlaybackCacheInvalidated Handle(SongDeleted message) =>
        new(message.BandId, "song", message.SongId, message.ContentVersion, "song_deleted");

    public static PlaybackCacheInvalidated Handle(SongRestored message) =>
        new(message.BandId, "song", message.SongId, message.ContentVersion, "song_restored");

    public static PlaybackCacheInvalidated Handle(SongSectionsChanged message) =>
        new(message.BandId, "song-sections", message.SongId, 0, "song_sections_changed");

    public static PlaybackCacheInvalidated Handle(PlaylistChanged message) =>
        new(message.BandId, "playlist", message.PlaylistId, message.ContentVersion,
            "playlist_changed");

    public static PlaybackCacheInvalidated Handle(PlaylistDeleted message) =>
        new(message.BandId, "playlist", message.PlaylistId, message.ContentVersion,
            "playlist_deleted");

    public static PlaybackCacheInvalidated Handle(StemsChanged message) =>
        new(message.BandId, "stems", message.SongId, message.ContentVersion, "stems_changed");
}
