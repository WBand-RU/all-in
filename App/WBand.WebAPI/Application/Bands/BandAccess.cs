using WBand.WebAPI.Data;

namespace WBand.WebAPI.Application.Bands;

internal static class BandAccess
{
    public static async Task<BandMember?> FindMembership(DataContext db, Guid bandId, Guid userId, CancellationToken ct)
    {
        if (db.HasTransaction) await db.LockBandAsync(bandId, ct);
        return await db.SingleOrDefaultAsync<BandMember>(
            "select * from public.band_members where band_id = @bandId and user_id = @userId", new { bandId, userId }, ct);
    }

    public static async Task<bool> IsOwner(DataContext db, Guid bandId, Guid userId, CancellationToken ct) =>
        (await FindMembership(db, bandId, userId, ct))?.Role == BandMemberRole.Owner;
}
