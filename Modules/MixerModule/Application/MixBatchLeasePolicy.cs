using WBand.Modules.MixerModule.Domain;

namespace WBand.Modules.MixerModule.Application;

/// <summary>Defines when a durable mixer batch may be claimed by a worker.</summary>
public static class MixBatchLeasePolicy
{
    public static bool CanClaim(MixBatch batch, DateTimeOffset now) =>
        batch.Status is MixBatchStatus.Queued or MixBatchStatus.Processing &&
        (batch.LeaseExpiresAt is null || batch.LeaseExpiresAt <= now);
}
