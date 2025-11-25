using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PlaybackService.Domain;

/// <summary>
/// Represents an individual audio track for a song
/// </summary>
public sealed class Track
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; set; }

    [BsonElement("song_id")]
    public required string SongId { get; set; }

    [BsonElement("instrument")]
    public required string Instrument { get; set; }

    [BsonElement("file_key")]
    public required string FileKey { get; set; }

    [BsonElement("duration_seconds")]
    public double DurationSeconds { get; set; }

    [BsonElement("created_at")]
    public required DateTime CreatedAt { get; set; }

    [BsonElement("created_by")]
    public required string CreatedBy { get; set; }

    [BsonElement("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [BsonElement("updated_by")]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Predefined instrument types for worship band tracks
    /// </summary>
    public static class Instruments
    {
        public const string Drums = "Drums";
        public const string Bass = "Bass";
        public const string Guitar = "Guitar";
        public const string Keys = "Keys";
        public const string Vocals = "Vocals";
        public const string Piano = "Piano";
        public const string Strings = "Strings";
        public const string Brass = "Brass";
        public const string Percussion = "Percussion";
        public const string Other = "Other";

        public static readonly string[] All =
        {
            Drums,
            Bass,
            Guitar,
            Keys,
            Vocals,
            Piano,
            Strings,
            Brass,
            Percussion,
            Other,
        };
    }
}
