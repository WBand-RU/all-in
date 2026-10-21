using Microsoft.EntityFrameworkCore;

namespace WBand.Modules.EventModule.Data;

/// <summary>Owns event persistence without dependencies on other modules' entities.</summary>
public sealed class EventDataContext(DbContextOptions<EventDataContext> options) : DbContext(options)
{
    public DbSet<PerformanceEvent> Events => Set<PerformanceEvent>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PerformanceEvent>();
        entity.ToTable("performance_events", "events");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.Title).HasMaxLength(200);
        entity.Property(x => x.Venue).HasMaxLength(500);
        entity.Property(x => x.Description).HasMaxLength(5000);
        entity.Property(x => x.SongIds).HasColumnType("uuid[]");
        entity.Property(x => x.Version).IsConcurrencyToken();
        entity.HasIndex(x => new { x.BandId, x.StartsAt });
    }
}
