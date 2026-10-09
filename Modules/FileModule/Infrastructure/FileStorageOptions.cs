using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Shared.Configuration;

namespace WBand.Modules.FileModule.Infrastructure;

/// <summary>
/// Defines the S3-compatible storage owned by the file module.
/// </summary>
public sealed class FileStorageOptions
{
    public const string SectionName = "Modules:FileModule:S3";

    [Required, Url]
    public string Url { get; set; } = string.Empty;

    [Required]
    public string Bucket { get; set; } = string.Empty;

    public string? Region { get; set; }

    [Required]
    public string AccessKey { get; set; } = string.Empty;

    [Required]
    public string SecretKey { get; set; } = string.Empty;

    [Range(1, 604800)]
    public int UploadExpirationSeconds { get; set; }

    [Range(1, 604800)]
    public int DownloadExpirationSeconds { get; set; }

    [Range(1, long.MaxValue)]
    public long MaxFileSizeBytes { get; set; } = 536_870_912;

    [Range(1, 1440)]
    public int IncompleteUploadLifetimeMinutes { get; set; } = 60;

    [Range(1, 1440)]
    public int CleanupIntervalMinutes { get; set; } = 15;

    [MinLength(1)]
    public string[] AllowedMimeTypes { get; set; } = ["audio/wav", "audio/x-wav", "audio/mpeg",
        "audio/ogg", "audio/opus", "audio/flac", "audio/x-flac", "audio/mp4", "audio/aac",
        "audio/x-ms-wma"];

    /// <summary>
    /// Reads development settings first and falls back to production environment variables.
    /// </summary>
    public static FileStorageOptions FromConfiguration(IConfiguration configuration)
    {
        return new FileStorageOptions
        {
            Url = GetRequired(configuration, "Url", "S3_HOST"),
            Bucket = GetRequired(configuration, "Bucket", "S3_BUCKET"),
            Region = ConfigurationValue.GetOptional(
                configuration,
                $"{SectionName}:Region",
                "S3_REGION"
            ),
            AccessKey = GetRequired(configuration, "AccessKey", "S3_ACCESS_KEY"),
            SecretKey = GetRequired(configuration, "SecretKey", "S3_SECRET_KEY"),
            UploadExpirationSeconds = int.Parse(
                GetRequired(
                    configuration,
                    "UploadExpirationSeconds",
                    "S3_UPLOAD_EXPIRATION_SECONDS"
                )
            ),
            DownloadExpirationSeconds = int.Parse(
                GetRequired(
                    configuration,
                    "DownloadExpirationSeconds",
                    "S3_DOWNLOAD_EXPIRATION_SECONDS"
                )
            ),
            MaxFileSizeBytes = configuration.GetValue<long?>(
                $"{SectionName}:MaxFileSizeBytes") ?? 536_870_912,
            IncompleteUploadLifetimeMinutes = configuration.GetValue<int?>(
                $"{SectionName}:IncompleteUploadLifetimeMinutes") ?? 60,
            CleanupIntervalMinutes = configuration.GetValue<int?>(
                $"{SectionName}:CleanupIntervalMinutes") ?? 15,
            AllowedMimeTypes = configuration.GetSection($"{SectionName}:AllowedMimeTypes")
                .Get<string[]>() ?? ["audio/wav", "audio/x-wav", "audio/mpeg", "audio/ogg",
                    "audio/opus", "audio/flac", "audio/x-flac", "audio/mp4", "audio/aac",
                    "audio/x-ms-wma"],
        };
    }

    private static string GetRequired(
        IConfiguration configuration,
        string optionName,
        string environmentVariable
    ) =>
        ConfigurationValue.GetRequired(
            configuration,
            $"{SectionName}:{optionName}",
            environmentVariable
        );
}
