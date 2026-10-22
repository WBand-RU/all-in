using System.Reflection;
using WBand.Modules.BandModule;
using WBand.Modules.BandModule.Contracts;
using WBand.WebAPI.Endpoints.Bands;
using Xunit;

namespace WBand.WebAPI.Tests;

public sealed class ModuleBoundaryTests
{
    [Fact]
    public void BandContracts_WhenReferenced_DoNotExposeHostOrPersistenceDependencies()
    {
        var contracts = typeof(CheckBandPermission).Assembly;
        Assert.Equal("WBand.Modules.BandModule.Contracts", contracts.GetName().Name);
        Assert.DoesNotContain(contracts.GetReferencedAssemblies(), reference =>
            reference.Name is "WBand.WebAPI" or "WBand.Modules.BandModule"
            || reference.Name!.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.All(contracts.GetExportedTypes(), type =>
            Assert.Equal("WBand.Modules.BandModule.Contracts", type.Namespace));
    }

    [Fact]
    public void BandPermissionQuery_WhenHandled_IsOwnedByBandModule()
    {
        Assert.Single(HandlersFor(typeof(BandModule).Assembly, typeof(CheckBandPermission)));
        Assert.Empty(HandlersFor(typeof(CreateBandEndpoint).Assembly, typeof(CheckBandPermission)));
    }

    [Fact]
    public void BandModule_WhenReferenced_DoesNotDependOnHostOrDownstreamImplementation()
    {
        Assert.DoesNotContain(typeof(BandModule).Assembly.GetReferencedAssemblies(), reference =>
            reference.Name is "WBand.WebAPI" or "WBand.AppHost"
                or "WBand.Modules.EventModule" or "WBand.Modules.SongModule");
    }

    private static IEnumerable<MethodInfo> HandlersFor(Assembly assembly, Type messageType) =>
        assembly.GetTypes().SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.Name == "Handle"
                && method.GetParameters().FirstOrDefault()?.ParameterType == messageType);
}
