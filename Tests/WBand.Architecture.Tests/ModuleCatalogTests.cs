using Shared.Modules;
using WBand.Modules.FileModule;
using WBand.Modules.UserModule;
using Xunit;

namespace WBand.Architecture.Tests;

public sealed class ModuleCatalogTests
{
    [Fact]
    public void Modules_HaveUniqueNamesAndAssemblies()
    {
        var catalog = new ModuleCatalog(new UserModule(), new FileModule());

        Assert.Equal(catalog.Modules.Count, catalog.Modules.Select(module => module.Name).Distinct().Count());
        Assert.Equal(
            catalog.Modules.Count,
            catalog.Modules.Select(module => module.Assembly).Distinct().Count()
        );
    }

    [Fact]
    public void Modules_DoNotComeFromLegacyServicesDirectory()
    {
        var catalog = new ModuleCatalog(new UserModule(), new FileModule());

        Assert.All(
            catalog.Modules,
            module => Assert.DoesNotContain("Services", module.Assembly.Location, StringComparison.Ordinal)
        );
    }
}
