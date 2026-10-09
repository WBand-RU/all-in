using System.Text.Json.Serialization;

namespace WBand.Modules.PlaylistModule.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlaylistTransitionType { AutoStart, Pause, Crossfade }

public sealed record PlaylistSectionOverride(string Name, int StartBar, int EndBar);
public sealed record StemMixOverride(Guid StemId, decimal Volume, decimal Pan);

public sealed class PlaylistItem
{
    public Guid Id { get; set; }
    public Guid SongId { get; set; }
    public int Order { get; set; }
    public string? KeyOverride { get; set; }
    public int? BpmOverride { get; set; }
    public List<PlaylistSectionOverride> StructureOverride { get; set; } = [];
    public List<StemMixOverride> StemMixOverrides { get; set; } = [];
    public PlaylistTransitionType Transition { get; set; } = PlaylistTransitionType.Pause;
    public int PauseSeconds { get; set; }
    public int CrossfadeSeconds { get; set; }
    public string? Notes { get; set; }
}

public sealed class Playlist
{
    public Guid Id { get; set; }
    public Guid BandId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset? EventDateTime { get; set; }
    public string? Venue { get; set; }
    public List<PlaylistItem> Items { get; set; } = [];
    public long ContentVersion { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public bool IsDeleted => DeletedAt is not null;
}
