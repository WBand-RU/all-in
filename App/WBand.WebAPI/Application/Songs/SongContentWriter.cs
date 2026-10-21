using WBand.WebAPI.Data;

namespace WBand.WebAPI.Application.Songs;

internal static class SongContentWriter
{
    // The caller holds the song lock, validates the revision and owns the transaction.
    public static async Task<SongContentCell> Save(DataContext db, SongContentCell? cell,
        Guid songId, Guid sectionId, Guid partitionId, string content, Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (cell is null)
        {
            cell = new SongContentCell
            {
                Id = Guid.CreateVersion7(), SongId = songId, SectionId = sectionId, PartitionId = partitionId,
                Content = content, CreatedAt = now, CreatedBy = userId,
            };
            await db.InsertAsync(cell, cancellationToken);
        }
        else
        {
            cell.Content = content;
            cell.Revision++;
            cell.UpdatedAt = now;
            cell.UpdatedBy = userId;
            await db.UpdateAsync(cell, cancellationToken);
        }
        return cell;
    }
}
