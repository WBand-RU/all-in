using System.Reflection;
using FluentValidation;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Modules;
using WBand.Modules.StemModule.Domain;

namespace WBand.Modules.StemModule;

/// <summary>Composes stem metadata, grouping, and playback-cache invalidation.</summary>
public sealed class StemModule : IWBandModule
{
    public string Name => "StemModule";
    public Assembly Assembly => typeof(StemModule).Assembly;
    public string MartenSchemaName => "stems";

    public void AddServices(IHostApplicationBuilder builder) =>
        builder.Services.AddValidatorsFromAssembly(Assembly);

    public void ConfigureMarten(StoreOptions options)
    {
        options.Schema.For<Stem>().DatabaseSchemaName(MartenSchemaName);
        options.Schema.For<StemGroup>().DatabaseSchemaName(MartenSchemaName);
        options.Schema.For<StemCollection>().DatabaseSchemaName(MartenSchemaName)
            .UseOptimisticConcurrency(true);
    }
}
