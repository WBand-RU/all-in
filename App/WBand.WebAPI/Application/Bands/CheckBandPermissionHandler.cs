using WBand.WebAPI.Data;
using WBand.Modules.BandModule.Contracts;

namespace WBand.WebAPI.Application.Bands;

public static class CheckBandPermissionHandler
{
    public static async Task<bool> Handle(
        CheckBandPermission query,
        DataContext db,
        CancellationToken cancellationToken
    )
    {
        var member = await BandAccess.FindMembership(
            db,
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
