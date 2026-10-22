namespace WBand.Contracts;

/// <summary>Platform roles supplied by the identity provider, independent of band membership.</summary>
public static class Roles
{
    /// <summary>Platform administration role.</summary>
    public const string SuperAdmin = "super-admin";
    /// <summary>Platform moderation role.</summary>
    public const string Moderator = "moderator";
}
