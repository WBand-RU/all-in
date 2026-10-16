using WBand.Modules.MixerModule.Domain;

namespace WBand.Modules.MixerModule.Application;

/// <summary>Defines queue eligibility; claiming also requires a PostgreSQL session lock.</summary>
public static class MixBatchLeasePolicy
{
    public static bool CanClaim(MixBatch batch, DateTimeOffset now) =>
        batch.Status is MixBatchStatus.Queued or MixBatchStatus.Processing &&
        (batch.NextAttemptAt is null || batch.NextAttemptAt <= now);
}
