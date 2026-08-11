namespace WBand.Modules.FileModule;

internal sealed class FileServiceConfig
{
    internal required string Bucket { get; set; }
    internal required int UploadExpirationSeconds { get; set; }
    internal required int DownloadExpirationSeconds { get; set; }
}
