using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Modules;
using WBand.Modules.UserModule.Infrastructure;
using WBand.Modules.UserModule.Infrastructure.Messaging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace WBand.Modules.UserModule;

/// <summary>
/// Composes user profile services and the Keycloak event listener.
/// </summary>
public sealed class UserModule : IWBandModule
{
    private static readonly BrokerName KeycloakBroker = new("keycloak-events");

    public string Name => "UserModule";

    public Assembly Assembly => typeof(UserModule).Assembly;

    public void AddServices(IHostApplicationBuilder builder)
    {
        var events = KeycloakEventsOptions.FromConfiguration(builder.Configuration);

        builder
            .Services.AddOptions<KeycloakEventsOptions>()
            .Configure(options => Copy(events, options))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    public void ConfigureWolverine(WolverineOptions options, IConfiguration configuration)
    {
        var events = KeycloakEventsOptions.FromConfiguration(configuration);

        var broker = options.AddNamedRabbitMqBroker(KeycloakBroker, factory =>
        {
            factory.HostName = events.Host;
            factory.Port = events.Port;
            factory.UserName = events.Username;
            factory.Password = events.Password;
            factory.VirtualHost = events.VirtualHost;
            factory.Ssl.Enabled = events.SslEnabled;
        });

        broker
            .AutoProvision()
            .BindExchange(events.Exchange, ExchangeType.Topic)
            .ToQueue(events.Queue, events.RoutingKey);

        options
            .ListenToRabbitQueueOnNamedBroker(KeycloakBroker, events.Queue)
            .DefaultIncomingMessage<KeycloakEvent>();
    }

    private static void Copy(KeycloakEventsOptions source, KeycloakEventsOptions target)
    {
        target.Host = source.Host;
        target.Port = source.Port;
        target.Username = source.Username;
        target.Password = source.Password;
        target.VirtualHost = source.VirtualHost;
        target.SslEnabled = source.SslEnabled;
        target.Realm = source.Realm;
        target.Exchange = source.Exchange;
        target.Queue = source.Queue;
    }
}
