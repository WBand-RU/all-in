using System.Reflection;
using FluentValidation;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Modules;
using WBand.Modules.BandModule.Domain;

namespace WBand.Modules.BandModule;

public sealed class BandModule : IWBandModule
{
    public string Name => "BandModule";
    public Assembly Assembly => typeof(BandModule).Assembly;
    public string MartenSchemaName => "bands";

    public void AddServices(IHostApplicationBuilder builder) =>
        builder.Services.AddValidatorsFromAssembly(Assembly);

    public void ConfigureMarten(StoreOptions options)
    {
        options.Schema.For<Band>().DatabaseSchemaName(MartenSchemaName);
        options.Schema.For<BandMember>().DatabaseSchemaName(MartenSchemaName);
        options.Schema.For<BandInvitation>().DatabaseSchemaName(MartenSchemaName);
    }
}
