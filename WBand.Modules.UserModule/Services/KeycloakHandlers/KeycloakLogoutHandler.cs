using System.Text.Json;
using WBand.Modules.UserModule.Events;
using Wolverine;
using static WBand.Modules.UserModule.Services.KeycloakMessagesReader;

namespace WBand.Modules.UserModule.Services.KeycloakHandlers;

internal class KeycloakLogoutHandler(IMessageBus messageBus) : IKeycloakHandler
{
    public string Key => "LOGOUT";

    public async Task Handle(string fullMessage)
    {
        throw new NotImplementedException();

        //var login =
        //    JsonSerializer.Deserialize<LoginMessage>(fullMessage)
        //    ?? throw new Exception("Keycloak login message is null");

        //await messageBus.PublishAsync(
        //    new UserLoggedIn(login.UserId, login.Details.Username, new DateTime(login.Time))
        //);
    }
}
