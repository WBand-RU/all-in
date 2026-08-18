using System.Reflection;
using FluentValidation;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Modules;
using WBand.Modules.PlaylistModule.Domain;

namespace WBand.Modules.PlaylistModule;

public sealed class PlaylistModule : IWBandModule
{
    public string Name => "PlaylistModule";
    public Assembly Assembly => typeof(PlaylistModule).Assembly;
    public string MartenSchemaName => "playlists";

    public void AddServices(IHostApplicationBuilder builder) =>
        builder.Services.AddValidatorsFromAssembly(Assembly);

    public void ConfigureMarten(StoreOptions options) =>
        options.Schema.For<Playlist>()
            .DatabaseSchemaName(MartenSchemaName)
            .UseOptimisticConcurrency(true);
}
