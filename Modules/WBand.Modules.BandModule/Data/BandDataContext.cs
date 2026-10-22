using WBand.Modules.BandModule.Contracts;
using Microsoft.EntityFrameworkCore;
using WBand.Persistence;

namespace WBand.Modules.BandModule.Data;

/// <summary>Owns profiles, bands, memberships, departures and invitations.</summary>
public sealed class BandDataContext(DbContextOptions<BandDataContext> options) : RelationalDataContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Band> Bands => Set<Band>();
    public DbSet<BandMember> BandMembers => Set<BandMember>();
    public DbSet<BandDeparture> BandDepartures => Set<BandDeparture>();
    public DbSet<BandInvitation> BandInvitations => Set<BandInvitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BandDataContext).Assembly);

    /// <inheritdoc />
    public override async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, CancellationToken ct = default)
    {
        var args = Parameters(parameters);
        object rows = typeof(T) switch
        {
            var type when type == typeof(User) => await Read<User>(sql, args, ct),
            var type when type == typeof(Band) => await Read<Band>(sql, args, ct),
            var type when type == typeof(BandMember) => await Read<BandMember>(sql, args, ct),
            var type when type == typeof(BandDeparture) => await Read<BandDeparture>(sql, args, ct),
            var type when type == typeof(BandInvitation) => await Read<BandInvitation>(sql, args, ct),
            _ => await base.QueryAsync<T>(sql, parameters, ct),
        };
        return (IReadOnlyList<T>)rows;
    }

    /// <summary>Serializes membership changes within an explicit transaction.</summary>
    public async Task LockBandAsync(Guid bandId, CancellationToken ct)
    {
        await LockResourceAsync(BandMutationLock.ResourceName(bandId), ct);
        await Bands.FromSqlInterpolated($"select * from bands where id = {bandId} for update")
            .AsNoTracking().ToListAsync(ct);
    }
}
