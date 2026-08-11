using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Shared.Modules;

/// <summary>
/// Composes registered modules without exposing their internal implementations.
/// </summary>
public sealed class ModuleCatalog(params IWBandModule[] modules)
{
    private readonly IReadOnlyList<IWBandModule> modules = modules;

    /// <summary>
    /// Gets all modules enabled for the current host.
    /// </summary>
    public IReadOnlyList<IWBandModule> Modules => modules;

    /// <summary>
    /// Registers services owned by every enabled module.
    /// </summary>
    public void AddServices(IHostApplicationBuilder builder)
    {
        foreach (var module in modules)
        {
            module.AddServices(builder);
        }
    }

    /// <summary>
    /// Maps non-Wolverine endpoints owned by enabled modules.
    /// </summary>
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        foreach (var module in modules)
        {
            module.MapEndpoints(endpoints);
        }
    }
}
