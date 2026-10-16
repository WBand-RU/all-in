using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WBand.WebAPI.Data;
using Xunit;

namespace WBand.WebAPI.Tests;

public sealed class DatabaseDependencyInjectionTests
{
    [Fact]
    public async Task AddDatabase_WhenScopesAreValidated_ResolvesOneContextPerScope()
    {
        var services = new ServiceCollection();
        services.AddDatabase("Host=localhost;Database=ef_di_test;Username=test;Password=test");
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<DataContext>());
        await using var first = provider.CreateAsyncScope();
        await using var second = provider.CreateAsyncScope();
        var db = first.ServiceProvider.GetRequiredService<DataContext>();
        Assert.Same(db, first.ServiceProvider.GetRequiredService<DataContext>());
        Assert.NotSame(db, second.ServiceProvider.GetRequiredService<DataContext>());
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", db.Database.ProviderName);
        Assert.Equal(8, db.Model.GetEntityTypes().Count());
    }
}
