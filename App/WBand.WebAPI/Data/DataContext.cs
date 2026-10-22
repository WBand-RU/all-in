using Microsoft.EntityFrameworkCore;
using WBand.Persistence;

namespace WBand.WebAPI.Data;

/// <summary>Persists song content owned by the Web API during module extraction.</summary>
public sealed partial class DataContext(DbContextOptions<DataContext> options) : RelationalDataContext(options)
{
    public DbSet<Song> Songs => Set<Song>();
    public DbSet<SongSection> SongSections => Set<SongSection>();
    public DbSet<SongContentCell> SongContentCells => Set<SongContentCell>();
    public DbSet<Partition> SongPartitions => Set<Partition>();
    public DbSet<SongRevision> SongRevisions => Set<SongRevision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }
}
