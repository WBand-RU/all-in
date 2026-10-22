namespace WBand.Contracts;

/// <summary>Authenticated identity supplied by the host to application modules.</summary>
public interface ICurrentUser
{
    /// <summary>Stable identity provider subject.</summary>
    Guid Id { get; }
    /// <summary>Authenticated email address.</summary>
    string Email { get; }
    /// <summary>Given name supplied by the identity provider.</summary>
    string FirstName { get; }
    /// <summary>Family name supplied by the identity provider.</summary>
    string LastName { get; }
    /// <summary>Reports whether the authenticated identity has a platform role.</summary>
    bool IsInRole(string role);
}
