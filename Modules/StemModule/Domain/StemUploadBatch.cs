namespace WBand.Modules.StemModule.Domain;

/// <summary>Distinguishes staged, atomically committed and cancelled folder uploads.</summary>
public enum StemUploadStatus { Uploading, Completed, Aborted }

/// <summary>Staged stems become visible together only after every file is verified.</summary>
public sealed class StemUploadBatch
{
    public Guid Id { get; set; }
    public Guid SongId { get; set; }
    public Guid BandId { get; set; }
    public Guid CreatedBy { get; set; }
    public StemUploadStatus Status { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CleanupUntil { get; set; }
    public bool CleanupFinished { get; set; }
    public List<StemUploadDraft> Files { get; set; } = [];
}

/// <summary>Owns the metadata and object key of a file in an uncommitted upload.</summary>
public sealed class StemUploadDraft
{
    public Stem Stem { get; set; } = new();
    public string ObjectKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Sha256 { get; set; } = string.Empty;
}
