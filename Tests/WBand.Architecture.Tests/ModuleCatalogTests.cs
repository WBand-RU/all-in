using Shared.Modules;
using WBand.Modules.FileModule;
using WBand.Modules.BandModule;
using WBand.Modules.UserModule;
using WBand.Modules.SongModule;
using WBand.Modules.PlaylistModule;
using WBand.Modules.StemModule;
using WBand.Modules.MixerModule;
using Xunit;

namespace WBand.Architecture.Tests;

public sealed class ModuleCatalogTests
{
    [Fact]
    public void Modules_HaveUniqueNamesAndAssemblies()
    {
        var catalog = CreateCatalog();

        Assert.Equal(catalog.Modules.Count, catalog.Modules.Select(module => module.Name).Distinct().Count());
        Assert.Equal(
            catalog.Modules.Count,
            catalog.Modules.Select(module => module.Assembly).Distinct().Count()
        );
        Assert.Equal(
            catalog.Modules.Count,
            catalog.Modules.Select(module => module.MartenSchemaName).Distinct().Count()
        );
    }

    [Fact]
    public void Modules_DoNotComeFromLegacyServicesDirectory()
    {
        var catalog = CreateCatalog();

        Assert.All(
            catalog.Modules,
            module => Assert.DoesNotContain("Services", module.Assembly.Location, StringComparison.Ordinal)
        );
    }

    private static ModuleCatalog CreateCatalog() => new(new UserModule(), new BandModule(),
        new SongModule(), new PlaylistModule(), new FileModule(), new StemModule(),
        new MixerModule());
}
