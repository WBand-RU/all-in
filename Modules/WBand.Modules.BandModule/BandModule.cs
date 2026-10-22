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
        services.AddDbContext<BandDataContext>(options => ConfigurePersistence(options, connectionString),
            optionsLifetime: ServiceLifetime.Singleton);
        return services;
    }

    /// <summary>Applies module migrations after the host has released its legacy band model.</summary>
    public static async Task ApplyMigrationsAsync(string connectionString, CancellationToken ct = default)
    {
        var options = new DbContextOptionsBuilder<BandDataContext>();
        ConfigurePersistence(options, connectionString);
        await using var db = new BandDataContext(options.Options);
        await db.Database.MigrateAsync(ct);
    }

    internal static void ConfigurePersistence(DbContextOptionsBuilder options, string connectionString)
    {
        var configured = new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = "bands,public" };
        options.UseNpgsql(configured.ConnectionString,
            postgres => postgres.MigrationsHistoryTable("__band_migrations", "bands"));
    }

    /// <summary>Includes module-owned message handlers in Wolverine discovery.</summary>
    public static void Configure(WolverineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Discovery.IncludeAssembly(typeof(BandModule).Assembly);
    }
}
