using Wolverine;
using WBand.Modules.BandModule.Data;
using WBand.Modules.BandModule.Contracts;

namespace WBand.Modules.BandModule.Application;

/// <summary>
/// Creates local profiles on registration and updates existing profiles on login.
/// </summary>
public static class KeycloakEventHandler
{
    /// <summary>
    /// Saves profile changes idempotently without requiring an outgoing notification.
    /// </summary>
    public static async Task Handle(
        KeycloakEvent message,
        BandDataContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(message.Type))
        {
            throw new InvalidOperationException("Keycloak event type is required.");
        }

        if (message.Type is not ("REGISTER" or "LOGIN"))
        {
            return;
        }

        if (message.UserId == Guid.Empty)
        {
            throw new InvalidOperationException("Keycloak user id is required.");
        }

        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        // Serializes registration/login for one subject, including when the user does not exist yet.
        await db.ExecuteAsync("select pg_advisory_xact_lock(hashtextextended(@subject, 0))",
            new { subject = message.UserId.ToString() }, cancellationToken);
        var occurredAt = DateTimeOffset.FromUnixTimeMilliseconds(message.Time);
        var user = await db.SingleOrDefaultAsync<User>(
            "select * from users where keycloak_id = @id", new { id = message.UserId }, cancellationToken);
        if (message.Type == "REGISTER")
        {
            if (user is not null) return;
            user = CreateUser(message, occurredAt);
            await db.InsertAsync(user, cancellationToken);
        }
        else
        {
            if (user is null)
                throw new InvalidOperationException("User must be registered before login can be processed.");
            if (occurredAt <= user.LastLoginAt) return;
            user.LastLoginAt = occurredAt;
            if (user.UpdatedAt < occurredAt) user.UpdatedAt = occurredAt;
            await db.UpdateAsync(user, cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    private static User CreateUser(KeycloakEvent message, DateTimeOffset occurredAt)
    {
        message.Details.TryGetValue("email", out var email);
        message.Details.TryGetValue("first_name", out var firstName);
        message.Details.TryGetValue("last_name", out var lastName);

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email is required.");
        }

        return new User
        {
            KeycloakId = message.UserId,
            Email = email.Trim().ToLowerInvariant(),
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
            LastLoginAt = default,
            FirstName = firstName?.Trim() ?? string.Empty,
            LastName = lastName?.Trim() ?? string.Empty,
        };
    }

}
