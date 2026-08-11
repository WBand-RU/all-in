namespace WBand.Modules.BandModule.Contracts;

public enum BandPermission
{
    View,
    EditContent,
    ManageBand,
}

public sealed record CheckBandPermission(Guid BandId, Guid UserId, BandPermission Permission);

public sealed record GetBandMemberInfo(Guid BandId, Guid UserId);

public sealed record GetUserBandIds(Guid UserId);

public sealed record BandMemberInfo(bool Found, Guid UserId, string? Email, string? Role)
{
    public static BandMemberInfo NotFound(Guid userId) => new(false, userId, null, null);
}
