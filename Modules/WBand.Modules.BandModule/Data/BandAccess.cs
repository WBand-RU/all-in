namespace WBand.Modules.BandModule.Data;

/// <summary>Reads and serializes membership access in module-owned persistence.</summary>
public static class BandAccess
{
    /// <summary>Reads current membership, retaining the band lock in an active transaction.</summary>
    public static async Task<BandMember?> FindMembership(BandDataContext db, Guid bandId, Guid userId, CancellationToken ct)
    {
        if (db.HasTransaction) await db.LockBandAsync(bandId, ct);
        return await db.SingleOrDefaultAsync<BandMember>(
            "select * from band_members where band_id = @bandId and user_id = @userId", new { bandId, userId }, ct);
    }

    /// <summary>Checks whether the current membership owns the band.</summary>
    public static async Task<bool> IsOwner(BandDataContext db, Guid bandId, Guid userId, CancellationToken ct) =>
        (await FindMembership(db, bandId, userId, ct))?.Role == BandMemberRole.Owner;
}
