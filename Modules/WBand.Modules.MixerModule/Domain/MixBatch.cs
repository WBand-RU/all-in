using System.Text.Json.Serialization;

namespace WBand.Modules.MixerModule.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MixBatchStatus { Queued, Processing, Ready, Failed }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MixArtifactKind { Full, Focus, Minus }

public sealed class MixBatch
{
    public Guid Id { get; set; }
    public Guid SongId { get; set; }
    public Guid BandId { get; set; }
    public MixBatchStatus Status { get; set; } = MixBatchStatus.Queued;
    public int SourceCount { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? HeartbeatAt { get; set; }
    public DateTimeOffset? LastEnqueuedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? CurrentStage { get; set; }
    public string? CurrentPlan { get; set; }
    public int CompletedOutputCount { get; set; }
    public int TotalOutputCount { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? LeaseId { get; set; }
    public DateTimeOffset? LeaseExpiresAt { get; set; }
}

public sealed class MixArtifact
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public Guid SongId { get; set; }
    public Guid BandId { get; set; }
    public Guid FileId { get; set; }
    public MixArtifactKind Kind { get; set; }
    public Guid? TargetStemId { get; set; }
    public string Group { get; set; } = "Общий микс";
    public string Name { get; set; } = string.Empty;
    public string Format { get; set; } = "mp3";
    public DateTimeOffset CreatedAt { get; set; }
}
