using BandService.Domain;
using MongoDB.Driver;

namespace BandService.Services;

/// <summary>
/// Service for handling permission checks
/// </summary>
public sealed class PermissionService(Repository repository)
{
    /// <summary>
    /// Gets the default permissions for a role
    /// </summary>
    public static Permission GetDefaultPermissions(MemberRole role)
    {
        return role switch
        {
            MemberRole.Owner => Permission.All,
            MemberRole.Admin => Permission.EditSongs
                | Permission.EditPlaylists
                | Permission.ManagePlaybacks
                | Permission.ManageAgents
                | Permission.SendMessages,
            MemberRole.Member => Permission.SendMessages,
            _ => Permission.None,
        };
    }

    /// <summary>
    /// Checks if a user has a specific permission in a band
    /// </summary>
    public async Task<bool> HasPermissionAsync(
        string userId,
        string bandId,
        Permission permission,
        CancellationToken cancellationToken = default
    )
    {
        var member = await repository
            .Members.Find(m => m.UserId == userId && m.BandId == bandId)
            .FirstOrDefaultAsync(cancellationToken);

        if (member == null)
            return false;

        // Owner always has all permissions
        if (member.Role == MemberRole.Owner)
            return true;

        // Check if member has the specific permission
        return (member.Permissions & permission) == permission;
    }

    /// <summary>
    /// Checks if a user is a member of a band
    /// </summary>
    public async Task<bool> IsMemberAsync(
        string userId,
        string bandId,
        CancellationToken cancellationToken = default
    )
    {
        var count = await repository.Members.CountDocumentsAsync(
            m => m.UserId == userId && m.BandId == bandId,
            cancellationToken: cancellationToken
        );

        return count > 0;
    }

    /// <summary>
    /// Gets a member's role in a band
    /// </summary>
    public async Task<MemberRole?> GetMemberRoleAsync(
        string userId,
        string bandId,
        CancellationToken cancellationToken = default
    )
    {
        var member = await repository
            .Members.Find(m => m.UserId == userId && m.BandId == bandId)
            .FirstOrDefaultAsync(cancellationToken);

        return member?.Role;
    }

    /// <summary>
    /// Updates a member's permissions
    /// </summary>
    public async Task UpdateMemberPermissionsAsync(
        string memberId,
        Permission permissions,
        CancellationToken cancellationToken = default
    )
    {
        await repository.Members.UpdateOneAsync(
            m => m.Id == memberId,
            Builders<Member>.Update.Set(m => m.Permissions, permissions),
            cancellationToken: cancellationToken
        );
    }
}
