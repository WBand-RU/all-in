using Wolverine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WBand.Modules.BandModule.Data;

namespace WBand.Modules.BandModule;

/// <summary>Registers the band module in the modular host.</summary>
public static class BandModule
{
    /// <summary>Registers scoped module persistence with an immutable PostgreSQL configuration.</summary>
    public static IServiceCollection AddBandModule(this IServiceCollection services, string connectionString)
    {
        var configured = new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = "bands,public" };
        services.AddDbContext<BandDataContext>(options => options.UseNpgsql(configured.ConnectionString,
            postgres => postgres.MigrationsHistoryTable("__band_migrations", "bands")),
            optionsLifetime: ServiceLifetime.Singleton);
        return services;
    }

    /// <summary>Includes module-owned message handlers in Wolverine discovery.</summary>
    public static void Configure(WolverineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Discovery.IncludeAssembly(typeof(BandModule).Assembly);
    }
}
