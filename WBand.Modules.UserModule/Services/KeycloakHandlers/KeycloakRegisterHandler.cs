using System.Text.Json;
using Wolverine;
using static WBand.Modules.UserModule.Services.KeycloakMessagesReader;

namespace WBand.Modules.UserModule.Services.KeycloakHandlers;

internal class KeycloakRegisterHandler(IMessageBus messageBus) : IKeycloakHandler
{
    public string Key => "REGISTER";

    public async Task Handle(string fullMessage)
    {
        var login = JsonSerializer.Deserialize<LoginMessage>(fullMessage);
        await messageBus.PublishAsync(login);
    }
}
