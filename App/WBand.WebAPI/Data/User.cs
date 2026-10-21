using System.Text.Json.Serialization;

namespace WBand.WebAPI.Data;

public class User
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public required Guid KeycloakId { get; init; }
    public required string Email { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; set; }
    public required DateTimeOffset LastLoginAt { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>Bands created by this user.</summary>
    [JsonIgnore]
    public ICollection<Band> CreatedBands { get; set; } = [];

    /// <summary>Active band memberships of this user.</summary>
    [JsonIgnore]
    public ICollection<BandMember> BandMemberships { get; set; } = [];

    /// <summary>Band invitations sent by this user.</summary>
    [JsonIgnore]
    public ICollection<BandInvitation> SentBandInvitations { get; set; } = [];

    /// <summary>Recorded departures of this user.</summary>
    [JsonIgnore]
    public ICollection<BandDeparture> BandDepartures { get; set; } = [];

    /// <summary>Songs created by this user.</summary>
    [JsonIgnore]
    public ICollection<Song> CreatedSongs { get; set; } = [];

    /// <summary>Songs last updated by this user.</summary>
    [JsonIgnore]
    public ICollection<Song> UpdatedSongs { get; set; } = [];

    /// <summary>Songs deleted by this user.</summary>
    [JsonIgnore]
    public ICollection<Song> DeletedSongs { get; set; } = [];

    /// <summary>Song partitions created by this user.</summary>
    [JsonIgnore]
    public ICollection<Partition> CreatedSongPartitions { get; set; } = [];

    /// <summary>Song partitions last updated by this user.</summary>
    [JsonIgnore]
    public ICollection<Partition> UpdatedSongPartitions { get; set; } = [];

    /// <summary>Shared song sections created by this user.</summary>
    [JsonIgnore]
    public ICollection<SongSection> CreatedSongSections { get; set; } = [];

    /// <summary>Shared song sections last updated by this user.</summary>
    [JsonIgnore]
    public ICollection<SongSection> UpdatedSongSections { get; set; } = [];

    /// <summary>Song metadata revisions created by this user.</summary>
    [JsonIgnore]
    public ICollection<SongRevision> CreatedSongRevisions { get; set; } = [];
}
