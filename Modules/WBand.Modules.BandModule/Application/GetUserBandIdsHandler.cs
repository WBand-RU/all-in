using WBand.Modules.BandModule.Data;
using WBand.Modules.BandModule.Contracts;

namespace WBand.Modules.BandModule.Application;

/// <summary>Answers the public band query using module-owned storage.</summary>
public static class GetUserBandIdsHandler
{
    /// <summary>Reads data visible through the query contract.</summary>
    public static async Task<Guid[]> Handle(GetUserBandIds query, BandDataContext db, CancellationToken cancellationToken) =>
        (await db.QueryAsync<Guid>("select distinct band_id from band_members where user_id = @UserId", query, cancellationToken)).ToArray();
}
