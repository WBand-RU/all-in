using Microsoft.Extensions.Configuration;
using Shared.Configuration;
using System.Text;
using Xunit;

namespace WBand.Architecture.Tests;

public sealed class ConfigurationValueTests
{
    [Fact]
    public void GetRequired_PrefersEnvironmentVariable()
    {
        var environmentVariable = $"WBAND_TEST_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(environmentVariable, "production-value");

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?> { ["Module:Option"] = "development-value" }
                )
                .Build();

            var value = ConfigurationValue.GetRequired(
                configuration,
                "Module:Option",
                environmentVariable
            );

            Assert.Equal("production-value", value);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariable, null);
        }
    }

    [Fact]
    public void GetRequired_UsesEnvironmentVariableWhenConfigurationIsMissing()
    {
        var environmentVariable = $"WBAND_TEST_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(environmentVariable, "production-value");

        try
        {
            var configuration = new ConfigurationBuilder().Build();

            var value = ConfigurationValue.GetRequired(
                configuration,
                "Module:Option",
                environmentVariable
            );

            Assert.Equal("production-value", value);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariable, null);
        }
    }

    [Fact]
    public void ConfigurationSources_PrioritizeEnvironmentThenEnvironmentFileThenBaseFile()
    {
        var environmentVariable = $"WBAND_TEST_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(environmentVariable, "environment");

        try
        {
            var baseJson = $$"""{ "{{environmentVariable}}": "base" }""";
            var environmentJson = $$"""{ "{{environmentVariable}}": "environment-file" }""";

            var configuration = new ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(baseJson)))
                .AddJsonStream(
                    new MemoryStream(Encoding.UTF8.GetBytes(environmentJson))
                )
                .AddEnvironmentVariables()
                .Build();

            Assert.Equal("environment", configuration[environmentVariable]);

            Environment.SetEnvironmentVariable(environmentVariable, null);

            configuration = new ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(baseJson)))
                .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(environmentJson)))
                .AddEnvironmentVariables()
                .Build();

            Assert.Equal("environment-file", configuration[environmentVariable]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariable, null);
        }
    }
}
