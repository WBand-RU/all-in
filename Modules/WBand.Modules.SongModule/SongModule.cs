using Wolverine;

namespace WBand.Modules.SongModule;

/// <summary>Registers the song module in the modular host.</summary>
public static class SongModule
{
    /// <summary>Includes module-owned message handlers in Wolverine discovery.</summary>
    public static void Configure(WolverineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Discovery.IncludeAssembly(typeof(SongModule).Assembly);
    }
}
