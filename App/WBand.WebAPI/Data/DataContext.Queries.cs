using WBand.Modules.BandModule.Contracts;
using Microsoft.EntityFrameworkCore;

namespace WBand.WebAPI.Data;

public sealed partial class DataContext
{
    /// <summary>Reads mapped entities or SQL projections through EF Core.</summary>
    public override async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, CancellationToken ct = default)
    {
        var args = Parameters(parameters);
        // Mapped entities must use their configured column names and conversions.
        object rows = typeof(T) switch
        {
            var type when type == typeof(Song) => await Read<Song>(sql, args, ct),
            var type when type == typeof(SongSection) => await Read<SongSection>(sql, args, ct),
            var type when type == typeof(SongContentCell) => await Read<SongContentCell>(sql, args, ct),
            var type when type == typeof(Partition) => await Read<Partition>(sql, args, ct),
            var type when type == typeof(SongRevision) => await Read<SongRevision>(sql, args, ct),
            _ => await Database.SqlQueryRaw<T>(sql, args).ToListAsync(ct),
        };
        return (IReadOnlyList<T>)rows;
    }

    /// <summary>Coordinates mutations within a band without reading another module's tables.</summary>
    public Task LockBandAsync(Guid bandId, CancellationToken ct) =>
        LockResourceAsync(BandMutationLock.ResourceName(bandId), ct);

}
