namespace WBand.WebProxy;

public sealed class Configuration
{
    [ConfigurationKeyName("WEB_COMMA_SPLIT_ADDRESSES")]
    public required string WebCommaSplitAddresses { get; init; }

    [ConfigurationKeyName("API_COMMA_SPLIT_ADDRESSES")]
    public required string ApiCommaSplitAddresses { get; init; }
}
