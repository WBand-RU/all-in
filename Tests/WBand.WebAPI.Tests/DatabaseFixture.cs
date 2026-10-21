using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Moq;
using Wolverine;
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
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:18.3").Build();
    public string ConnectionString => postgres.GetConnectionString();
    public ServiceProvider Services { get; private set; } = null!;
    public RecordingPublisher Publisher { get; } = new();
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
        await postgres.DisposeAsync();
    }
}

public sealed class RecordingPublisher
{
    public System.Collections.Concurrent.ConcurrentDictionary<Guid, object> Messages { get; } = new();
    public Mock<IMessageBus> Bus { get; } = new();
    public RecordingPublisher()
    {
        Capture<WBand.Modules.UserModule.Events.UserCreated>();
        Capture<WBand.Modules.UserModule.Events.UserLoggedIn>();
        Capture<object>();
    }

    private void Capture<T>() where T : class =>
        Bus.Setup(bus => bus.PublishAsync(It.IsAny<T>(), It.IsAny<DeliveryOptions>()))
            .Callback<T, DeliveryOptions>((message, _) => Messages[Guid.NewGuid()] = message)
            .Returns(ValueTask.CompletedTask);
}
