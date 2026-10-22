using Wolverine;

namespace WBand.Modules.BandModule;

/// <summary>Registers the band module in the modular host.</summary>
public static class BandModule
{
    /// <summary>Includes module-owned message handlers in Wolverine discovery.</summary>
    public static void Configure(WolverineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Discovery.IncludeAssembly(typeof(BandModule).Assembly);
    }
}
