using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WBand.Modules.SongModule.Data;

/// <summary>Creates the song context for migration scaffolding without starting the host.</summary>
public sealed class SongDataContextDesignTimeFactory : IDesignTimeDbContextFactory<SongDataContext>
{
    /// <inheritdoc />
    public SongDataContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SongDataContext>();
        SongModule.ConfigurePersistence(options, "Host=localhost;Database=wband;Username=postgres");
        return new SongDataContext(options.Options);
    }
}
