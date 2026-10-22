using Wolverine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WBand.WebAPI.Application.Songs;
using DataContext = WBand.Modules.SongModule.Data.SongDataContext;
using WBand.WebAPI.Services;
using Wolverine.Http;

namespace WBand.WebAPI.Endpoints.Songs;

public static class ListSongPartitionsEndpoint
{
    [Authorize, Tags("SongPartitions"), ProducesResponseType<PartitionResponse[]>(200)]
    [WolverineGet("/song-api/songs/{songId}/partitions", OperationId = "GetSongPartitions")]
    public static async Task<IResult> Get(Guid songId, ICurrentUser user, [FromServices] DataContext db, [FromServices] IMessageBus bus, CancellationToken cancellationToken)
    {
        var song = await SongAccess.Load(db, songId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, bus, song.BandId, user.Id, false, cancellationToken)) return Results.Forbid();
        var partitions = await db.QueryAsync<Partition>(
            "select * from song_partitions where song_id = @songId order by \"order\", created_at, id", new { songId }, cancellationToken);
        return Results.Ok(partitions.Select(PartitionResponse.From).ToArray());
    }
}

public static class CreatePartitionEndpoint
{
    [Authorize, Tags("SongPartitions"), ProducesResponseType<PartitionResponse>(200)]
    [WolverinePost("/song-api/songs/{songId}/partitions", OperationId = "CreatePartition")]
    public static async Task<IResult> Post(Guid songId, CreatePartitionRequest request, ICurrentUser user,
        [FromServices] DataContext db, [FromServices] IMessageBus bus, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        var song = await SongAccess.Load(db, songId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, bus, song.BandId, user.Id, true, cancellationToken)) return Results.Forbid();
        var partition = new Partition
        {
            Id = Guid.CreateVersion7(), SongId = songId, Name = request.Name.Trim(), Order = request.Order,
            CreatedAt = DateTimeOffset.UtcNow, CreatedBy = user.Id,
        };
        await db.InsertAsync(partition, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Ok(PartitionResponse.From(partition));
    }
}

public static class UpdatePartitionEndpoint
{
    [Authorize, Tags("SongPartitions"), ProducesResponseType<PartitionResponse>(200)]
    [WolverinePut("/song-api/partitions/{partitionId}", OperationId = "UpdatePartition")]
    public static async Task<IResult> Put(Guid partitionId, UpdatePartitionRequest request, ICurrentUser user,
        [FromServices] DataContext db, [FromServices] IMessageBus bus, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        var partition = await db.FindAsync<Partition>(partitionId, cancellationToken);
        if (partition is null) return Results.NotFound();
        var song = await SongAccess.Load(db, partition.SongId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, bus, song.BandId, user.Id, true, cancellationToken)) return Results.Forbid();
        partition = await db.FindAsync<Partition>(partitionId, cancellationToken);
        if (partition is null) return Results.NotFound();
        if (partition.Revision != request.ExpectedRevision)
            return Results.Conflict(new { code = "song_partition_version_conflict", currentRevision = partition.Revision });
        partition.Name = request.Name.Trim();
        partition.Order = request.Order;
        partition.Revision++;
        partition.UpdatedAt = DateTimeOffset.UtcNow;
        partition.UpdatedBy = user.Id;
        await db.UpdateAsync(partition, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Ok(PartitionResponse.From(partition));
    }
}

public static class DeletePartitionEndpoint
{
    [Authorize, Tags("SongPartitions"), ProducesResponseType(204)]
    [WolverineDelete("/song-api/partitions/{partitionId}", OperationId = "DeletePartition")]
    public static async Task<IResult> Delete(Guid partitionId, ICurrentUser user,
        [FromServices] DataContext db, [FromServices] IMessageBus bus, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        var partition = await db.FindAsync<Partition>(partitionId, cancellationToken);
        if (partition is null) return Results.NotFound();
        var song = await SongAccess.Load(db, partition.SongId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, bus, song.BandId, user.Id, true, cancellationToken)) return Results.Forbid();
        if (await db.DeleteAsync<Partition>(partitionId, cancellationToken) == 0) return Results.NotFound();
        await transaction.CommitAsync(cancellationToken);
        return Results.NoContent();
    }
}
