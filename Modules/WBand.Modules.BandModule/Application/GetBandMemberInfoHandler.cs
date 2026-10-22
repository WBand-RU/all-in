using WBand.Modules.BandModule.Data;
using WBand.Modules.BandModule.Contracts;

namespace WBand.Modules.BandModule.Application;

/// <summary>Answers the public band query using module-owned storage.</summary>
public static class GetBandMemberInfoHandler
{
    /// <summary>Reads data visible through the query contract.</summary>
    public static async Task<BandMemberInfo> Handle(
        GetBandMemberInfo query,
        BandDataContext db,
        CancellationToken cancellationToken)
    {
        var member = await BandAccess.FindMembership(db, query.BandId, query.UserId, cancellationToken);

        return member is null
            ? BandMemberInfo.NotFound(query.UserId)
            : new BandMemberInfo(true, member.UserId, member.Email, member.Role.ToString());
    }
}
