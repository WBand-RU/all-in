using System.Security.Cryptography;
using System.Text;
using WBand.Modules.MixerModule.Domain;

namespace WBand.Modules.MixerModule.Application;

/// <summary>Counts logical mixes and gives each output a stable identity across retries.</summary>
public static class MixProgress
{
    /// <summary>A mix is complete when both its WAV and MP3 outputs have been committed.</summary>
    public static int CountCompleted(IReadOnlyList<MixPlan> plans, IReadOnlySet<string> outputs) =>
        plans.Count(plan => outputs.Contains(Key(ToArtifactKind(plan.Kind), plan.Target?.StemId, "wav"))
            && outputs.Contains(Key(ToArtifactKind(plan.Kind), plan.Target?.StemId, "mp3")));

    /// <summary>Identifies a logical output independently of filename, label or retry number.</summary>
    public static string Key(MixArtifactKind kind, Guid? targetStemId, string format) =>
        $"{kind}:{targetStemId?.ToString("D") ?? "full"}:{format.ToLowerInvariant()}";

    /// <summary>Deterministic document identity prevents retry-created duplicate artifacts.</summary>
    public static Guid ArtifactId(Guid batchId, MixArtifactKind kind, Guid? targetStemId, string format) =>
        new(SHA256.HashData(Encoding.UTF8.GetBytes($"{batchId:D}:{Key(kind, targetStemId, format)}"))
            .AsSpan(0, 16));

    internal static MixArtifactKind ToArtifactKind(MixPlanKind kind) => kind switch
    {
        MixPlanKind.Focus => MixArtifactKind.Focus,
        MixPlanKind.Minus => MixArtifactKind.Minus,
        _ => MixArtifactKind.Full,
    };
}
