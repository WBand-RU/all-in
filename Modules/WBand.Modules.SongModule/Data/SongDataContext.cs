using Microsoft.EntityFrameworkCore;
using WBand.Modules.BandModule.Contracts;
using WBand.Persistence;

namespace WBand.Modules.SongModule.Data;

/// <summary>Owns song metadata, matrix axes, text cells and revision history.</summary>
public sealed class SongDataContext(DbContextOptions<SongDataContext> options) : RelationalDataContext(options)
{
    public DbSet<Song> Songs => Set<Song>();
    public DbSet<SongSection> SongSections => Set<SongSection>();
    public DbSet<Partition> SongPartitions => Set<Partition>();
    public DbSet<SongContentCell> SongContentCells => Set<SongContentCell>();
    public DbSet<SongRevision> SongRevisions => Set<SongRevision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SongDataContext).Assembly);

    /// <inheritdoc />
    public override async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, CancellationToken ct = default)
    {
        var args = Parameters(parameters);
        object rows = typeof(T) switch
        {
            var type when type == typeof(Song) => await Read<Song>(sql, args, ct),
            var type when type == typeof(SongSection) => await Read<SongSection>(sql, args, ct),
            var type when type == typeof(Partition) => await Read<Partition>(sql, args, ct),
            var type when type == typeof(SongContentCell) => await Read<SongContentCell>(sql, args, ct),
            var type when type == typeof(SongRevision) => await Read<SongRevision>(sql, args, ct),
            _ => await base.QueryAsync<T>(sql, parameters, ct),
        };
        return (IReadOnlyList<T>)rows;
    }

    /// <summary>Coordinates song mutations with membership changes in the same band.</summary>
    public Task LockBandAsync(Guid bandId, CancellationToken ct) =>
        LockResourceAsync(BandMutationLock.ResourceName(bandId), ct);
}
