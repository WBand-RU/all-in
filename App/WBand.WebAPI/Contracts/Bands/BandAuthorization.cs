namespace WBand.Modules.BandModule.Contracts;

/// <summary>Permissions that other features can query through Wolverine.</summary>
public enum BandPermission
{
    View,
    EditContent,
    ManageBand,
}

/// <summary>Checks access for a user identified by their Keycloak subject.</summary>
public sealed record CheckBandPermission(Guid BandId, Guid UserId, BandPermission Permission);

/// <summary>Retrieves an active member's role and email.</summary>
public sealed record GetBandMemberInfo(Guid BandId, Guid UserId);

/// <summary>Retrieves band IDs for the user's active memberships.</summary>
public sealed record GetUserBandIds(Guid UserId);

/// <summary>Reports whether membership exists and its current permissions.</summary>
public sealed record BandMemberInfo(bool Found, Guid UserId, string? Email, string? Role)
{
    public static BandMemberInfo NotFound(Guid userId) => new(false, userId, null, null);
}
