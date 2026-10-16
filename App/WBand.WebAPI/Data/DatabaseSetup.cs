using Microsoft.EntityFrameworkCore;

namespace WBand.WebAPI.Data;

/// <summary>Registers EF Core persistence and applies database migrations.</summary>
public static class DatabaseSetup
{
    /// <summary>Registers a scoped PostgreSQL context.</summary>
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        string connectionString
    )
    {
        // Options only capture immutable connection settings. Singleton options let Wolverine
        // inject the scoped context without locating an opaque scoped options factory.
        services.AddDbContext<DataContext>(
            options => options.UseNpgsql(connectionString),
            optionsLifetime: ServiceLifetime.Singleton
        );
        return services;
    }

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
    }
}
