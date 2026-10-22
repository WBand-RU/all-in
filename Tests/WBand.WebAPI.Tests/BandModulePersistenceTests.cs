using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WBand.Modules.BandModule;
using WBand.Modules.BandModule.Data;
using Xunit;

namespace WBand.WebAPI.Tests;

public sealed class BandModulePersistenceTests
{
    [Fact]
    public void AddBandModule_WhenScopesAreValidated_ResolvesOneContextPerScope()
    {
        using var services = CreateServices();
        using var first = services.CreateScope();
        using var second = services.CreateScope();
        var context = first.ServiceProvider.GetRequiredService<BandDataContext>();
        Assert.Same(context, first.ServiceProvider.GetRequiredService<BandDataContext>());
        Assert.NotSame(context, second.ServiceProvider.GetRequiredService<BandDataContext>());
        Assert.NotNull(services.GetRequiredService<DbContextOptions<BandDataContext>>());
    }

    [Fact]
    public void Model_WhenMapped_ContainsOnlyOwnedEntitiesAndUsesKeycloakSubjects()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var model = scope.ServiceProvider.GetRequiredService<BandDataContext>().Model;
        Assert.Equal(5, model.GetEntityTypes().Count());
        Assert.All(model.GetEntityTypes(), entity =>
            Assert.Equal(typeof(BandDataContext).Assembly, entity.ClrType.Assembly));
        var actorKeys = model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys())
            .Where(key => key.PrincipalEntityType.ClrType == typeof(User)).ToArray();
        Assert.Equal(4, actorKeys.Length);
        Assert.All(actorKeys, key =>
            Assert.Equal(nameof(User.KeycloakId), Assert.Single(key.PrincipalKey.Properties).Name));
    }

    [Fact]
    public async Task Insert_WhenTransactionIsMissing_RejectsWriteBeforeConnecting()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BandDataContext>();
        var user = new User
        {
            KeycloakId = Guid.NewGuid(), Email = "test@example.invalid",
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
            LastLoginAt = default,
        };
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => context.InsertAsync(user));
        Assert.Contains("explicit transaction", error.Message);
    }

    private static ServiceProvider CreateServices() => new ServiceCollection()
        .AddLogging()
        .AddBandModule("Host=localhost;Database=wband_context_test;Username=unused")
        .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
}
