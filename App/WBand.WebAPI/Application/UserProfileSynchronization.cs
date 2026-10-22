using WBand.Modules.BandModule.Data;
using WBand.WebAPI.Services;
using Wolverine;

namespace WBand.WebAPI.Application;

/// <summary>Restores missing local profiles from the authenticated identity.</summary>
public static class UserProfileSynchronization
{
    /// <summary>Returns the existing profile or creates it once if registration delivery was missed.</summary>
    public static async Task<User> EnsureProfile(
        ICurrentUser identity, BandDataContext db, IMessageBus bus, CancellationToken cancellationToken)
    {
        var subject = identity.Id;
        var user = await db.SingleOrDefaultAsync<User>(
            "select * from users where keycloak_id = @id", new { id = subject }, cancellationToken);
        if (user is not null) return user;

        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        // Use the same lock as KeycloakEventHandler to serialize late REGISTER events and concurrent requests.
        await db.ExecuteAsync("select pg_advisory_xact_lock(hashtextextended(@subject, 0))",
            new { subject = subject.ToString() }, cancellationToken);
        user = await db.SingleOrDefaultAsync<User>(
            "select * from users where keycloak_id = @id", new { id = subject }, cancellationToken);
        if (user is not null) return user;

        var email = identity.Email;
        if (subject == Guid.Empty || string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("A valid authenticated subject and email are required.");

        var now = DateTimeOffset.UtcNow;
        user = new User
        {
            KeycloakId = subject,
            Email = email.Trim().ToLowerInvariant(),
            FirstName = identity.FirstName.Trim(),
            LastName = identity.LastName.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            LastLoginAt = now,
        };
        await db.InsertAsync(user, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return user;
    }
}
