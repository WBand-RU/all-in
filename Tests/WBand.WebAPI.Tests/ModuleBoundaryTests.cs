using WBand.Modules.SongModule;
using WBand.Modules.SongModule.Contracts;
using System.Reflection;
using WBand.Modules.BandModule;
using WBand.Modules.BandModule.Contracts;
using Xunit;

namespace WBand.WebAPI.Tests;

public sealed class ModuleBoundaryTests
{
    [Fact]
    public void SharedContracts_WhenReferenced_DoNotDependOnTheHostOrPersistence()
    {
        var assembly = typeof(ICurrentUser).Assembly;
        Assert.Equal("WBand.Contracts", assembly.GetName().Name);
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference =>
            reference.Name!.StartsWith("WBand.", StringComparison.Ordinal)
            || reference.Name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
            || reference.Name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

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
    public void SongContracts_WhenReferenced_DoNotExposeHostOrPersistenceDependencies()
    {
        var contracts = typeof(GetSongReferences).Assembly;
        Assert.Equal("WBand.Modules.SongModule.Contracts", contracts.GetName().Name);
        Assert.DoesNotContain(contracts.GetReferencedAssemblies(), reference =>
            reference.Name is "WBand.WebAPI" or "WBand.Modules.SongModule"
            || reference.Name!.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.All(contracts.GetExportedTypes(), type =>
            Assert.Equal("WBand.Modules.SongModule.Contracts", type.Namespace));
    }

    [Fact]
    public void SongModels_WhenReferenced_AreOwnedBySongModule()
    {
        var owner = typeof(SongModule).Assembly;
        foreach (var type in new[] { typeof(Song), typeof(SongSection), typeof(Partition), typeof(SongContentCell), typeof(SongRevision) })
            Assert.Equal(owner, type.Assembly);
    }

    [Fact]
    public void SongModule_WhenReferenced_DoesNotDependOnHostOrOtherModuleImplementations()
    {
        Assert.DoesNotContain(typeof(SongModule).Assembly.GetReferencedAssemblies(), reference =>
            reference.Name is "WBand.WebAPI" or "WBand.AppHost"
                or "WBand.Modules.EventModule" or "WBand.Modules.BandModule");
    }

    [Theory]
    [InlineData(typeof(CheckBandPermission))]
    [InlineData(typeof(GetBandMemberInfo))]
    [InlineData(typeof(GetUserBandIds))]
    [InlineData(typeof(GetBandContributorInfo))]
    [InlineData(typeof(KeycloakEvent))]
    public void BandMessage_WhenHandled_IsOwnedByBandModule(Type query)
    {
        Assert.Single(HandlersFor(typeof(BandModule).Assembly, query));
        Assert.Empty(HandlersFor(typeof(WBand.WebAPI.Data.DataContext).Assembly, query));
    }

    [Fact]
    public void BandModule_WhenReferenced_DoesNotDependOnHostOrDownstreamImplementation()
    {
        Assert.DoesNotContain(typeof(BandModule).Assembly.GetReferencedAssemblies(), reference =>
            reference.Name is "WBand.WebAPI" or "WBand.AppHost"
                or "WBand.Modules.EventModule" or "WBand.Modules.SongModule");
    }

    [Fact]
    public void BandHttp_WhenDiscovered_IsOwnedByTheModuleAndAbsentFromTheHost()
    {
        var moduleRoutes = BandRoutes(typeof(BandModule).Assembly).ToArray();
        Assert.Contains("/band-api/bands", moduleRoutes);
        Assert.Contains("/band-api/user/band-access", moduleRoutes);
        Assert.Contains("/api/user/me", moduleRoutes);
        Assert.Empty(BandRoutes(typeof(WBand.WebAPI.Data.DataContext).Assembly));
    }

    [Fact]
    public void Host_WhenBandFeaturesAreExtracted_DoesNotInjectBandPersistence()
    {
        var types = typeof(WBand.WebAPI.Data.DataContext).Assembly.GetTypes();
        var storage = typeof(WBand.Modules.BandModule.Data.BandDataContext);
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        Assert.DoesNotContain(types.SelectMany(type => type.GetMethods(flags)).SelectMany(method => method.GetParameters()),
            parameter => parameter.ParameterType == storage);
        Assert.DoesNotContain(types.SelectMany(type => type.GetConstructors(flags)).SelectMany(constructor => constructor.GetParameters()),
            parameter => parameter.ParameterType == storage);
        Assert.DoesNotContain(types.SelectMany(type => type.GetFields(flags)), field => field.FieldType == storage);
    }

    [Fact]
    public void ApplicationHandlers_WhenSongPersistenceIsHandedOver_DoNotInjectTheLegacyContext()
    {
        var legacy = typeof(WBand.WebAPI.Data.DataContext);
        var handlers = legacy.Assembly.GetTypes().Where(type =>
            type.Namespace?.StartsWith("WBand.WebAPI.Application", StringComparison.Ordinal) == true
            || type.Namespace?.StartsWith("WBand.WebAPI.Endpoints", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(handlers.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .SelectMany(method => method.GetParameters()), parameter => parameter.ParameterType == legacy);
    }

    private static IEnumerable<string> BandRoutes(Assembly assembly) => assembly.GetTypes()
        .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
        .SelectMany(method => method.GetCustomAttributesData())
        .Where(attribute => attribute.AttributeType.Namespace == "Wolverine.Http")
        .Select(attribute => attribute.ConstructorArguments.FirstOrDefault().Value).OfType<string>()
        .Where(route => route.StartsWith("/band-api/", StringComparison.Ordinal) || route == "/api/user/me");

    private static IEnumerable<MethodInfo> HandlersFor(Assembly assembly, Type messageType) =>
        assembly.GetTypes().SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.Name == "Handle"
                && method.GetParameters().FirstOrDefault()?.ParameterType == messageType);
}
