using System.Reflection;
using FluentValidation;
using ImTools;
using JasperFx.Core;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Services;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.ApiVersioning;
using Wolverine.Marten;
using Wolverine.RabbitMQ;

namespace Shared;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddShared(
        this IHostApplicationBuilder builder,
        Assembly assembly
    )
    {
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();

        builder.Services.AddValidatorsFromAssembly(assembly);

        return builder;
    }

    public static void AddEventDriven(
        this IHostApplicationBuilder builder,
        string rabbitmqHost,
        ushort rabbitmqPort,
        string rabbitmqUsername,
        string rabbitmqPassword,
        string martenDatabaseConnectionString,
        string martenDatabaseSchemaName,
        Assembly assembly,
        params Type[] describeTypes
    )
    {
        builder.UseWolverine(x =>
        {
            x.CodeGeneration.TypeLoadMode = JasperFx.CodeGeneration.TypeLoadMode.Static;

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

            x.Discovery.IncludeAssembly(assembly);

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

            x.UseFluentValidation();
        });
    }

    public static void UseWolverineEndpoints(this WebApplication app)
    {
        app.MapWolverineEndpoints(opts =>
        {
            opts.UseApiVersioning(x =>
            {
                x.UnversionedPolicy = UnversionedPolicy.RequireExplicit;
            });

            opts.UseDataAnnotationsValidationProblemDetailMiddleware();
        });
    }
}
