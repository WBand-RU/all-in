using System.Text.Json;
using Wolverine;
using static WBand.Modules.UserModule.Services.KeycloakMessagesReader;

namespace WBand.Modules.UserModule.Services.KeycloakHandlers;

internal class KeycloakLoginHandler(IMessageBus messageBus) : IKeycloakHandler
{
    public string Key => "LOGIN";

    public async Task Handle(string fullMessage)
    {
        var login = JsonSerializer.Deserialize<LoginMessage>(fullMessage);
        await messageBus.PublishAsync(login);
    }
}
