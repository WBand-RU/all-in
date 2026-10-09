using System.Reflection;
using FluentValidation;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Modules;
using WBand.Modules.SongModule.Domain;

namespace WBand.Modules.SongModule;

public sealed class SongModule : IWBandModule
{
    public string Name => "SongModule";
    public Assembly Assembly => typeof(SongModule).Assembly;
    public string MartenSchemaName => "songs";

    public void AddServices(IHostApplicationBuilder builder) =>
        builder.Services.AddValidatorsFromAssembly(Assembly);

    public void ConfigureMarten(StoreOptions options)
    {
        options.Schema.For<Song>().DatabaseSchemaName(MartenSchemaName).UseOptimisticConcurrency(true);
        options.Schema.For<SongSection>().DatabaseSchemaName(MartenSchemaName)
            .UseOptimisticConcurrency(true);
        options.Schema.For<SongRevision>().DatabaseSchemaName(MartenSchemaName);
    }
}
