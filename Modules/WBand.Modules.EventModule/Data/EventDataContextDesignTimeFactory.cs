using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WBand.Modules.EventModule.Data;

/// <summary>Constructs the event context for migration tooling without starting the host.</summary>
public sealed class EventDataContextDesignTimeFactory : IDesignTimeDbContextFactory<EventDataContext>
{
    public EventDataContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<EventDataContext>().UseNpgsql(
            "Host=localhost;Database=wband;Username=postgres;Password=postgres",
            postgres => postgres.MigrationsHistoryTable("__event_migrations", "events")).Options);
}
