using Shared;

namespace BandService.Domain;

public class Band
{
    public required BandId Id { get; init; }
    public BandName Name { get; private set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public required UserId CreatedBy { get; init; }
    public DateTime? UpdateAt { get; private set; }
    public UserId? UpdateBy { get; private set; }

    public void SetName(BandName newName, UserId userId)
    {
        Name = newName;
        UpdateAt = DateTime.UtcNow;
        UpdateBy = userId;
    }
}
