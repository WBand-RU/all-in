using Microsoft.EntityFrameworkCore;

namespace WBand.WebAPI.Data;

/// <summary>Persists Web API users, memberships and song content.</summary>
public sealed partial class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Band> Bands => Set<Band>();
    public DbSet<BandMember> BandMembers => Set<BandMember>();
    public DbSet<BandInvitation> BandInvitations => Set<BandInvitation>();
    public DbSet<BandDeparture> BandDepartures => Set<BandDeparture>();
    public DbSet<Song> Songs => Set<Song>();
    public DbSet<SongSection> SongSections => Set<SongSection>();
    public DbSet<SongRevision> SongRevisions => Set<SongRevision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }
}
