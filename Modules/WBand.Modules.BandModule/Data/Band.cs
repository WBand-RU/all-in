namespace WBand.Modules.BandModule.Data;

/// <summary>
/// A band whose memberships define access to its content.
/// </summary>
public sealed class Band
{
    public Guid Id { get; init; }
    public required string Name { get; set; }
    public Guid CreatedBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
