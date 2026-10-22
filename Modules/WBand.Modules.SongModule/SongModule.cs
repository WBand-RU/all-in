using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WBand.Modules.SongModule.Data;
using Wolverine;

namespace WBand.Modules.SongModule;

/// <summary>Registers the song module in the modular host.</summary>
public static class SongModule
{
    /// <summary>Registers scoped song persistence with immutable connection options.</summary>
    public static IServiceCollection AddSongModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SongDataContext>(options => ConfigurePersistence(options, connectionString),
            optionsLifetime: ServiceLifetime.Singleton);
        return services;
    }

    internal static void ConfigurePersistence(DbContextOptionsBuilder options, string connectionString)
    {
        var configured = new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = "songs,public" };
        options.UseNpgsql(configured.ConnectionString,
            postgres => postgres.MigrationsHistoryTable("__song_migrations", "songs"));
    }

    /// <summary>Includes module-owned message handlers in Wolverine discovery.</summary>
    public static void Configure(WolverineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Discovery.IncludeAssembly(typeof(SongModule).Assembly);
    }
}
