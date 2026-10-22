using WBand.Modules.BandModule.Data;
using ModuleBandAccess = WBand.Modules.BandModule.Data.BandAccess;
using WBand.Modules.BandModule.Contracts;

namespace WBand.WebAPI.Application.Bands;

public static class GetBandMemberInfoHandler
{
    public static async Task<BandMemberInfo> Handle(
        GetBandMemberInfo query,
        BandDataContext db,
        CancellationToken cancellationToken)
    {
        var member = await ModuleBandAccess.FindMembership(db, query.BandId, query.UserId, cancellationToken);

        return member is null
            ? BandMemberInfo.NotFound(query.UserId)
            : new BandMemberInfo(true, member.UserId, member.Email, member.Role.ToString());
    }
}
