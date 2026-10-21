using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WBand.Modules.EventModule.Data;

namespace WBand.Modules.EventModule;

/// <summary>Registers and initializes the event module.</summary>
public static class EventModule
{
    /// <summary>Registers event persistence in the modular host.</summary>
    public static IServiceCollection AddEventModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<EventDataContext>(options => options.UseNpgsql(connectionString,
            postgres => postgres.MigrationsHistoryTable("__event_migrations", "events")),
            optionsLifetime: ServiceLifetime.Singleton);
        return services;
    }

    /// <summary>Applies migrations owned by the event module.</summary>
    public static async Task MigrateEvents(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<EventDataContext>().Database.MigrateAsync(cancellationToken);
    }
}
