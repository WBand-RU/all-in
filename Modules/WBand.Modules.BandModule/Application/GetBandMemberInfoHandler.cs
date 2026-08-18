using Marten;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.BandModule.Domain;

namespace WBand.Modules.BandModule.Application;

public static class GetBandMemberInfoHandler
{
    public static async Task<BandMemberInfo> Handle(
        GetBandMemberInfo query,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        var member = await session.Query<BandMember>().FirstOrDefaultAsync(
            x => x.BandId == query.BandId && x.UserId == query.UserId,
            cancellationToken);

        return member is null
            ? BandMemberInfo.NotFound(query.UserId)
            : new BandMemberInfo(true, member.UserId, member.Email, member.Role.ToString());
    }
}
