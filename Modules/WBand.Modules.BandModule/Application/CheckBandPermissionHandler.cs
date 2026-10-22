using WBand.Modules.BandModule.Contracts;
using Wolverine;

namespace WBand.Modules.BandModule.Application;

/// <summary>Owns band authorization decisions without exposing persistence models.</summary>
public static class CheckBandPermissionHandler
{
    /// <summary>Checks the active membership obtained through the public module contract.</summary>
    public static async Task<bool> Handle(
        CheckBandPermission query, IMessageBus bus, CancellationToken cancellationToken)
    {
        var member = await bus.InvokeAsync<BandMemberInfo>(
            new GetBandMemberInfo(query.BandId, query.UserId), cancellationToken);
        if (!member.Found) return false;

        return query.Permission switch
        {
            BandPermission.View => true,
            BandPermission.EditContent => member.Role is "Owner" or "Admin",
            BandPermission.ManageBand => member.Role is "Owner",
            _ => false,
        };
    }
}
