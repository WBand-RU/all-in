using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using WBand.WebAPI.Data;
using Xunit;

namespace WBand.WebAPI.Tests;

[CollectionDefinition("Database", DisableParallelization = true)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}

public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:18.3")
        .WithCommand("-c", "log_connections=on", "-c", "log_disconnections=on")
        .Build();
    public string ConnectionString => postgres.GetConnectionString();
    public ServiceProvider Services { get; private set; } = null!;
    public DataContext CreateSession() => new(new DbContextOptionsBuilder<DataContext>().UseNpgsql(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDatabase(ConnectionString);
        Services = services.BuildServiceProvider();
        await DatabaseSetup.ApplyMigrations(ConnectionString, default);
        await DatabaseSetup.ApplyMigrations(ConnectionString, default);
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        try
        {
            await PostgresDiagnostics.SaveLogs(postgres);
        }
        finally
        {
            await postgres.DisposeAsync();
        }
    }
}
