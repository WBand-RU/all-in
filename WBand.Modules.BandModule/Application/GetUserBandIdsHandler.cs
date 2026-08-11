using Marten;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.BandModule.Domain;

namespace WBand.Modules.BandModule.Application;

public static class GetUserBandIdsHandler
{
    public static async Task<Guid[]> Handle(
        GetUserBandIds query,
        IQuerySession session,
        CancellationToken cancellationToken) =>
        (await session.Query<BandMember>()
            .Where(x => x.UserId == query.UserId)
            .ToListAsync(cancellationToken))
        .Select(x => x.BandId)
        .Distinct()
        .ToArray();
}
