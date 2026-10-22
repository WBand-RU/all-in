namespace WBand.Modules.BandModule.Contracts;

/// <summary>Identifies the shared transaction resource for mutations within one band.</summary>
public static class BandMutationLock
{
    /// <summary>Returns the stable resource name used by participating module transactions.</summary>
    public static string ResourceName(Guid bandId) => $"band:{bandId:D}";
}
