using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WBand.WebAPI.Data;
using Xunit;
using BandDataContext = WBand.Modules.BandModule.Data.BandDataContext;

namespace WBand.WebAPI.Tests;

public sealed class DatabaseDependencyInjectionTests
{
    [Fact]
    public void AddApplicationPersistence_WhenScopesAreValidated_RegistersOnlyModuleContexts()
    {
        using var provider = new ServiceCollection().AddLogging()
            .AddApplicationPersistence("Host=localhost;Database=ef_di_test;Username=test")
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        Assert.Null(provider.GetService<DataContext>());
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<SongDataContext>());
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var songs = first.ServiceProvider.GetRequiredService<SongDataContext>();
        var bands = first.ServiceProvider.GetRequiredService<BandDataContext>();
        Assert.Same(songs, first.ServiceProvider.GetRequiredService<SongDataContext>());
        Assert.NotSame(songs, second.ServiceProvider.GetRequiredService<SongDataContext>());
        Assert.NotSame(bands, second.ServiceProvider.GetRequiredService<BandDataContext>());
        Assert.Equal(5, songs.Model.GetEntityTypes().Count());
        Assert.Equal(5, bands.Model.GetEntityTypes().Count());
    }
}
