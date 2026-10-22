using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WBand.Modules.BandModule.Data;

/// <summary>Creates the module context for migration scaffolding without starting the host.</summary>
public sealed class BandDataContextDesignTimeFactory : IDesignTimeDbContextFactory<BandDataContext>
{
    /// <inheritdoc />
    public BandDataContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BandDataContext>();
        BandModule.ConfigurePersistence(options, "Host=localhost;Database=wband;Username=postgres");
        return new BandDataContext(options.Options);
    }
}
