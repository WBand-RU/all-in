using System.Reflection;
using JasperFx.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

namespace MessagesCommon;

public static class ServiceCollectionExtensions
{
    public static void AddMessaging(
        this IServiceCollection services,
        string rabbitmqHost,
        ushort rabbitmqPort,
        string rabbitmqUsername,
        string rabbitmqPassword,
        string martenDatabaseConnectionString,
        string martenDatabaseSchemaName,
        Assembly[] assemblies,
        params Type[] describeTypes
    )
    {
        services.AddWolverine(x =>
        {
            x.Policies.OnAnyException()
                .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds());

            x.UseRabbitMq(x =>
                {
                    x.HostName = rabbitmqHost;
                    x.Port = rabbitmqPort;
                    x.UserName = rabbitmqUsername;
                    x.Password = rabbitmqPassword;
                })
                .AutoProvision()
                .UseConventionalRouting();

            x.Policies.UseDurableOutboxOnAllSendingEndpoints();
            x.Policies.UseDurableInboxOnAllListeners();
            x.Policies.AutoApplyTransactions();

            x.EnvelopeIdGeneration = EnvelopeIdGeneration.GuidV7;

            foreach (var assembly in assemblies)
            {
                x.Discovery.IncludeAssembly(assembly);
            }

            foreach (var describeType in describeTypes)
            {
                var errorDesc = x.DescribeHandlerMatch(describeType);
                Console.WriteLine($"{describeType.Name} => {errorDesc}");
            }

            x.Services.AddMarten(x =>
                {
                    x.Connection(martenDatabaseConnectionString);

                    x.DisableNpgsqlLogging = false; // TODO: try use true
                })
                .IntegrateWithWolverine(x =>
                {
                    x.MessageStorageSchemaName = martenDatabaseSchemaName;
                });
        });
    }
}
