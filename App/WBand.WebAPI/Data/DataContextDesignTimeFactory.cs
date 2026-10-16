using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WBand.WebAPI.Data;

public sealed class DataContextDesignTimeFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<DataContext> builder = new();
        builder.UseNpgsql(
            "Server=localhost;Port=25432;Database=wband;Username=postgres;Password=postgres;"
        );

        return new DataContext(builder.Options);
    }
}
