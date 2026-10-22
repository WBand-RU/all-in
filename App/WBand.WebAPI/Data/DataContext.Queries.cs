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
            var type when type == typeof(User) => await Read<User>(sql, args, ct),
            var type when type == typeof(Band) => await Read<Band>(sql, args, ct),
            var type when type == typeof(BandMember) => await Read<BandMember>(sql, args, ct),
            var type when type == typeof(BandInvitation) => await Read<BandInvitation>(sql, args, ct),
            var type when type == typeof(BandDeparture) => await Read<BandDeparture>(sql, args, ct),
            var type when type == typeof(Song) => await Read<Song>(sql, args, ct),
            var type when type == typeof(SongSection) => await Read<SongSection>(sql, args, ct),
            var type when type == typeof(SongContentCell) => await Read<SongContentCell>(sql, args, ct),
            var type when type == typeof(Partition) => await Read<Partition>(sql, args, ct),
            var type when type == typeof(SongRevision) => await Read<SongRevision>(sql, args, ct),
            _ => await Database.SqlQueryRaw<T>(sql, args).ToListAsync(ct),
        };
        return (IReadOnlyList<T>)rows;
    }

    /// <summary>Serializes membership changes and invitation acceptance for a band.</summary>
    public async Task LockBandAsync(Guid bandId, CancellationToken ct)
    {
        RequireTransaction();
        await Bands.FromSqlInterpolated($"select * from public.bands where id = {bandId} for update")
            .AsNoTracking().ToListAsync(ct);
    }

}
