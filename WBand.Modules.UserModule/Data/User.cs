using BandService.Contracts;
using JasperFx.Events;
using WBand.Modules.UserModule.Domain;

namespace WBand.Modules.UserModule.Data;

public class User
{
    public static User Create(KeycloakUserId keycloakId, Email email)
    {
        return new User()
        {
            Id = new UserId(Guid.CreateVersion7()),
            KeycloakId = keycloakId,
            Email = email,
            UpdateAt = DateTime.UtcNow,
        };
    }

    public required UserId Id { get; init; }
    public required KeycloakUserId KeycloakId { get; init; }
    public Email Email { get; private set; }
    public DateTime RegisteredAt { get; } = DateTime.UtcNow;
    public DateTime? UpdateAt { get; private set; }

    public void SetName(Email newEmail)
    {
        this.Email = newEmail;
        this.UpdateAt = DateTime.UtcNow;
    }
}
