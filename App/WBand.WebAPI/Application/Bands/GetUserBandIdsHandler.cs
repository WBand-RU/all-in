using WBand.WebAPI.Data;
using WBand.Modules.BandModule.Contracts;

namespace WBand.WebAPI.Application.Bands;

public static class GetUserBandIdsHandler
{
    public static async Task<Guid[]> Handle(GetUserBandIds query, DataContext db, CancellationToken cancellationToken) =>
        (await db.QueryAsync<Guid>("select distinct band_id from public.band_members where user_id = @UserId", query, cancellationToken)).ToArray();
}
