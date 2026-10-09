using System.Text.Json.Serialization;

namespace WBand.Modules.FileModule.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FileObjectStatus { Pending, Ready, Rejected }

/// <summary>Requests a constrained upload owned by another application module.</summary>
public sealed record CreateFileUpload(Guid FileId, Guid BandId, string ObjectKey,
    string OriginalFileName, string MimeType, long Size, string Sha256, Guid CreatedBy);

public sealed record FileUploadTicket(Guid FileId, string UploadUrl,
    DateTimeOffset ExpiresAt, string MimeType, long Size);

public sealed record CreateFileUploadResult(FileUploadTicket? Ticket, string? Error)
{
    public static CreateFileUploadResult Success(FileUploadTicket ticket) => new(ticket, null);
    public static CreateFileUploadResult Rejected(string error) => new(null, error);
}

public sealed record GetFileReferences(IReadOnlyList<Guid> FileIds);

public sealed record FileReference(Guid FileId, Guid BandId, FileObjectStatus Status,
    string MimeType, long Size, string Sha256);

/// <summary>Requests short-lived read access for trusted background handlers.</summary>
public sealed record GetReadyFileDownloads(IReadOnlyList<Guid> FileIds);
public sealed record FileDownloadAccess(Guid FileId, Guid BandId, string DownloadUrl,
    string MimeType, long Size, string Sha256);

/// <summary>Completes and verifies an upload created by another module.</summary>
public sealed record CompleteFileUpload(Guid FileId);
public sealed record CompleteFileUploadResult(FileReference? File, string? Error)
{
    public static CompleteFileUploadResult Success(FileReference file) => new(file, null);
    public static CompleteFileUploadResult Rejected(string error) => new(null, error);
}
