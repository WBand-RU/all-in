using Microsoft.EntityFrameworkCore;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.BandModule.Data;

namespace WBand.Modules.BandModule.Application;

/// <summary>Projects contributor identities entirely within band-owned persistence.</summary>
public static class GetBandContributorInfoHandler
{
    /// <summary>Returns each known profile once with its role in the requested band.</summary>
    public static async Task<BandContributorInfo[]> Handle(
        GetBandContributorInfo query, BandDataContext db, CancellationToken ct)
    {
        if (query.UserIds.Length == 0) return [];
        var rows = await (
            from user in db.Users.AsNoTracking()
            join membership in db.BandMembers.Where(member => member.BandId == query.BandId)
                on user.KeycloakId equals membership.UserId into memberships
            from membership in memberships.DefaultIfEmpty()
            where query.UserIds.Contains(user.KeycloakId)
            select new { user.KeycloakId, user.Email, Role = (BandMemberRole?)membership.Role }
        ).ToArrayAsync(ct);
        return rows.Select(row => new BandContributorInfo(row.KeycloakId, row.Email, row.Role?.ToString())).ToArray();
    }
}
