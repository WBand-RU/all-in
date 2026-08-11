using Marten;
using WBand.Modules.BandModule.Domain;

namespace WBand.Modules.BandModule.Application;

internal static class BandAccess
{
    public static Task<BandMember?> FindMembership(
        IQuerySession session,
        Guid bandId,
        Guid userId,
        CancellationToken cancellationToken
    ) => session.Query<BandMember>()
        .OrderByDescending(x => x.JoinedAt)
        .FirstOrDefaultAsync(x => x.BandId == bandId && x.UserId == userId, cancellationToken);

    public static async Task<bool> IsOwner(
        IQuerySession session,
        Guid bandId,
        Guid userId,
        CancellationToken cancellationToken
    ) => (await FindMembership(session, bandId, userId, cancellationToken))?.Role
        == BandMemberRole.Owner;
}
