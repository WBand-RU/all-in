using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Wolverine;

namespace Shared.Modules;

/// <summary>
/// Defines the composition boundary of a WBand application module.
/// </summary>
public interface IWBandModule
{
    /// <summary>
    /// Gets the stable module name used in diagnostics and configuration.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the module assembly containing Wolverine handlers and HTTP endpoints.
    /// </summary>
    Assembly Assembly { get; }

    /// <summary>
    /// Registers module-owned services and validated configuration.
    /// </summary>
    void AddServices(IHostApplicationBuilder builder);

    /// <summary>
    /// Configures module-owned Wolverine listeners and routes.
    /// </summary>
    void ConfigureWolverine(WolverineOptions options, IConfiguration configuration) { }

    /// <summary>
    /// Maps endpoints that cannot be discovered by Wolverine HTTP.
    /// </summary>
    void MapEndpoints(IEndpointRouteBuilder endpoints) { }
}
