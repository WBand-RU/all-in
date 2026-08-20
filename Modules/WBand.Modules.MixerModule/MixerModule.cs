using System.Reflection;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Modules;
using WBand.Modules.MixerModule.Application;
using WBand.Modules.MixerModule.Contracts;
using WBand.Modules.MixerModule.Domain;
using Wolverine;

namespace WBand.Modules.MixerModule;

/// <summary>Generates rehearsal focus and minus mixes on a durable sequential worker queue.</summary>
public sealed class MixerModule : IWBandModule
{
    public string Name => "MixerModule";
    public Assembly Assembly => typeof(MixerModule).Assembly;
    public string MartenSchemaName => "mixer";

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.Services.AddHttpClient("mixer", client =>
            client.Timeout = TimeSpan.FromHours(2));
        builder.Services.AddHostedService<MixBatchRecoveryService>();
    }

    public void ConfigureWolverine(WolverineOptions options, IConfiguration configuration)
    {
        options.PublishMessage<GenerateSongMixes>().ToLocalQueue("mix-generation");
        options.LocalQueue("mix-generation").Sequential().UseDurableInbox();
    }

    public void ConfigureMarten(StoreOptions options)
    {
        options.Schema.For<MixBatch>().DatabaseSchemaName(MartenSchemaName);
        options.Schema.For<MixArtifact>().DatabaseSchemaName(MartenSchemaName);
    }
}
