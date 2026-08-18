using Microsoft.Extensions.Logging;
using WBand.Modules.UserModule.Infrastructure.Messaging;

namespace WBand.Modules.UserModule.Application;

/// <summary>
/// Accepts Keycloak events through Wolverine; profile synchronization is implemented in phase two.
/// </summary>
public static class KeycloakEventHandler
{
    /// <summary>
    /// Validates and records receipt of an external Keycloak event.
    /// </summary>
    public static void Handle(KeycloakEvent message, ILogger<KeycloakEventLog> logger)
    {
        if (string.IsNullOrWhiteSpace(message.Type))
        {
            throw new InvalidOperationException("Keycloak event type is required.");
        }

        logger.LogInformation(
            "Received Keycloak event {EventType} for user {KeycloakUserId}",
            message.Type,
            message.UserId
        );
    }

    public sealed class KeycloakEventLog;
}
