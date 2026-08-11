using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Shared.Configuration;

namespace WBand.Modules.FileModule.Infrastructure;

/// <summary>
/// Defines the S3-compatible storage owned by the file module.
/// </summary>
internal sealed class FileStorageOptions
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
