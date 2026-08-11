using Marten;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.BandModule.Domain;

namespace WBand.Modules.BandModule.Application;

public static class CheckBandPermissionHandler
{
    public static async Task<bool> Handle(
        CheckBandPermission query,
        IQuerySession session,
        CancellationToken cancellationToken
    )
    {
        var member = await BandAccess.FindMembership(
            session,
            query.BandId,
            query.UserId,
            cancellationToken
        );

        return query.Permission switch
        {
            BandPermission.View => member is not null,
            BandPermission.EditContent => member?.Role is BandMemberRole.Owner or BandMemberRole.Admin,
            BandPermission.ManageBand => member?.Role is BandMemberRole.Owner,
            _ => false,
        };
    }
}
