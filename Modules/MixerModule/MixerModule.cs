using System.Reflection;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Modules;
using WBand.Modules.MixerModule.Domain;

namespace WBand.Modules.MixerModule;

/// <summary>Owns rehearsal mix metadata and the FFmpeg processing pipeline.</summary>
public sealed class MixerModule : IWBandModule
{
    public string Name => "MixerModule";
    public Assembly Assembly => typeof(MixerModule).Assembly;
    public string MartenSchemaName => "mixer";

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.Services.AddHttpClient("mixer", client =>
            client.Timeout = TimeSpan.FromHours(2));
    }

    public void ConfigureMarten(StoreOptions options)
    {
        options.Schema.For<MixBatch>().DatabaseSchemaName(MartenSchemaName)
            .UseOptimisticConcurrency(true);
        options.Schema.For<MixArtifact>().DatabaseSchemaName(MartenSchemaName);
    }
}
