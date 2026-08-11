namespace WBand.Modules.UserModule.Services.KeycloakHandlers;

internal interface IKeycloakHandler
{
    string Key { get; }
    Task Handle(string fullMessage);
}
