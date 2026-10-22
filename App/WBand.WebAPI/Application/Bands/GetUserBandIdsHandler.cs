using WBand.Modules.BandModule.Data;
using WBand.Modules.BandModule.Contracts;

namespace WBand.WebAPI.Application.Bands;

public static class GetUserBandIdsHandler
{
    public static async Task<Guid[]> Handle(GetUserBandIds query, BandDataContext db, CancellationToken cancellationToken) =>
        (await db.QueryAsync<Guid>("select distinct band_id from band_members where user_id = @UserId", query, cancellationToken)).ToArray();
}
