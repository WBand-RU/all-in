using System.Reflection;
using Marten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.HealthChecks;
using Shared.Modules;
using WBand.Modules.UserModule.Data;
using WBand.Modules.UserModule.Domain;
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
    public void ConfigureWolverine(WolverineOptions options, IConfiguration configuration)
    {
        var events = KeycloakEventsOptions.FromConfiguration(configuration);

        var broker = options.AddNamedRabbitMqBroker(
            KeycloakBroker,
            factory =>
            {
                factory.HostName = events.Host;
                factory.Port = events.Port;
                factory.UserName = events.Username;
                factory.Password = events.Password;
                factory.VirtualHost = events.VirtualHost;
                factory.Ssl.Enabled = events.SslEnabled;
            }
        );

        broker
            .AutoProvision()
            .BindExchange(events.Exchange, ExchangeType.Topic)
            .ToQueue(events.Queue, events.RoutingKey);

        options
            .ListenToRabbitQueueOnNamedBroker(KeycloakBroker, events.Queue)
            .DefaultIncomingMessage<KeycloakEvent>();
    }

    public void ConfigureMarten(StoreOptions options)
    {
        options
            .Schema.For<UserProfile>()
            .DatabaseSchemaName(MartenSchemaName)
            .UseOptimisticConcurrency(true);
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
