using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Shared;
using WBand.Modules.UserModule.Configurations;
using WBand.Modules.UserModule.Services;
using WBand.Modules.UserModule.Services.KeycloakHandlers;

namespace WBand.Modules.UserModule;

public static class DependencyInjectionExtensions
{
    public static void AddUserModule(this IHostApplicationBuilder builder)
    {
        const string configPrefix = "Modules:UserModule:RabbitMQ";
        const string envPrefix = "WBAND_MODULES_USER_MODULE_MESSAGING";
        var rabbitmqHost =
            builder.Configuration[$"{configPrefix}:Host"]
            ?? EnvironmentVariable.Get($"{envPrefix}_HOST");
        var rabbitmqPort = ushort.Parse(
            builder.Configuration[$"{configPrefix}:Port"]
                ?? EnvironmentVariable.Get($"{envPrefix}_PORT")
        );
        var rabbitmqUsername =
            builder.Configuration[$"{configPrefix}:Username"]
            ?? EnvironmentVariable.Get($"{envPrefix}_USERNAME");
        var rabbitmqPassword =
            builder.Configuration[$"{configPrefix}:Password"]
            ?? EnvironmentVariable.Get($"{envPrefix}_PASSWORD");
        var rabbitmqVirtualHost =
            builder.Configuration[$"{configPrefix}:VirtualHost"]
            ?? EnvironmentVariable.Get($"{envPrefix}_VIRTUAL_HOST");
        var rabbitmqSslEnabled = bool.Parse(
            builder.Configuration[$"{configPrefix}:SslEnabled"]
                ?? EnvironmentVariable.Get($"{envPrefix}_SSL_ENABLED")
        );

        builder.Services.AddRabbitMQService();

        builder.Services.AddSingleton<IConnectionFactory>(
            (sp) =>
            {
                return new ConnectionFactory()
                {
                    HostName = rabbitmqHost,
                    Port = rabbitmqPort,
                    UserName = rabbitmqUsername,
                    Password = rabbitmqPassword,
                    VirtualHost = rabbitmqVirtualHost,
                    Ssl = { Enabled = rabbitmqSslEnabled },
                };
            }
        );
        builder.Services.AddHostedService<KeycloakMessagesReader>();

        var keycloakRealm =
            builder.Configuration["Modules:UserModule:KeycloakRealm"]
            ?? EnvironmentVariable.Get("WBAND_MODULES_USER_MODULE_KEYCLOAK_REALM");
        var exchange =
            builder.Configuration["Modules:UserModule:Exchange"]
            ?? EnvironmentVariable.Get("WBAND_MODULES_USER_MODULE_EXCHANGE");
        builder.Services.Configure<KeycloakMessagesReaderConfiguration>(options =>
        {
            options.Realm = keycloakRealm;
            options.Exchange = exchange;
        });

        RegisterKeycloakHandlers(builder.Services);
    }

    private static void RegisterKeycloakHandlers(IServiceCollection services)
    {
        services.AddTransient<IKeycloakHandler, KeycloakLoginHandler>();
        services.AddTransient<IKeycloakHandler, KeycloakRegisterHandler>();
        services.AddTransient<IKeycloakHandler, KeycloakLogoutHandler>();
    }
}
