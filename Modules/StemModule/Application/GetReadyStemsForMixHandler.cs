using Marten;
using WBand.Modules.FileModule.Contracts;
using WBand.Modules.StemModule.Contracts;
using WBand.Modules.StemModule.Domain;
using Wolverine;

namespace WBand.Modules.StemModule.Application;

public static class GetReadyStemsForMixHandler
{
    public static async Task<ReadyStemForMix[]> Handle(GetReadyStemsForMix query,
        IQuerySession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        var stems = await session.Query<Stem>().Where(stem => stem.SongId == query.SongId)
            .OrderBy(stem => stem.Name).ToListAsync(cancellationToken);
        var files = (await bus.InvokeAsync<FileReference[]>(new GetFileReferences(
            stems.Select(stem => stem.FileId).ToArray()))).ToDictionary(file => file.FileId);
        return stems.Where(stem => files.GetValueOrDefault(stem.FileId)?.Status == FileObjectStatus.Ready)
            .Select(stem => new ReadyStemForMix(stem.Id, stem.SongId, stem.BandId, stem.FileId,
                stem.Name, stem.Kind.ToString())).ToArray();
    }
}
