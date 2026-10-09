using WBand.Modules.FileModule.Contracts;

namespace WBand.Modules.FileModule.Domain;

/// <summary>Describes a private object whose lifecycle is controlled by WBand.</summary>
public sealed class FileObject
{
    public Guid Id { get; set; }
    public Guid BandId { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public FileObjectStatus Status { get; set; } = FileObjectStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? RejectionReason { get; set; }
}
