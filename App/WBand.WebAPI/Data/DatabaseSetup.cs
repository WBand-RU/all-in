using WBand.Modules.SongModule;
using WBand.Modules.BandModule;
using Microsoft.EntityFrameworkCore;

namespace WBand.WebAPI.Data;

/// <summary>Registers EF Core persistence and applies database migrations.</summary>
public static class DatabaseSetup
{
    /// <summary>Registers only the module-owned application contexts.</summary>
    public static IServiceCollection AddApplicationPersistence(this IServiceCollection services, string connectionString) =>
        services.AddBandModule(connectionString).AddSongModule(connectionString);

    /// <summary>Applies pending EF Core migrations.</summary>
    public static async Task ApplyMigrations(
        string connectionString,
        CancellationToken cancellationToken
    )
    {
        using var db = new DataContext(
            new DbContextOptionsBuilder<DataContext>().UseNpgsql(connectionString).Options
        );
        await db.Database.MigrateAsync(cancellationToken);
        await BandModule.ApplyMigrationsAsync(connectionString, cancellationToken);
    }
}
