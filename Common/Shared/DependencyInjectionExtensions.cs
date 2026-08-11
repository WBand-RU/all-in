using JasperFx.Core;
using Marten;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Shared.Configuration;
using Shared.HealthChecks;
using Shared.Modules;
using Shared.Services;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Marten;
using Wolverine.RabbitMQ;

namespace Shared;

/// <summary>
/// Registers common application infrastructure for the modular monolith host.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Configures sources with the priority: environment variables, environment file, then appsettings.json.
    /// </summary>
    public static WebApplicationBuilder ApplyWBandConfiguration(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        builder.Configuration.AddJsonFile(
            $"appsettings.{builder.Environment.EnvironmentName}.json",
            optional: true,
            reloadOnChange: true
        );
        builder.Configuration.AddEnvironmentVariables();

        return builder;
    }

    /// <summary>
    /// Registers shared services, validated options, Wolverine, Marten, and operational endpoints.
    /// </summary>
    public static WebApplicationBuilder AddWBandFoundation(
        this WebApplicationBuilder builder,
        ModuleCatalog modules
    )
    {
        var messaging = MessagingOptions.FromConfiguration(builder.Configuration);
        var marten = MartenOptions.FromConfiguration(builder.Configuration);

        builder
            .Services.AddOptions<MessagingOptions>()
            .Configure(options => Copy(messaging, options))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder
            .Services.AddOptions<MartenOptions>()
            .Configure(options => Copy(marten, options))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck(
                "postgresql",
                new PostgreSqlHealthCheck(marten.ConnectionString),
                tags: ["ready"]
            )
            .AddCheck(
                "rabbitmq",
                new TcpEndpointHealthCheck(messaging.Host, messaging.Port),
                tags: ["ready"]
            );
        builder.Services.AddWolverineHttp();

        Log.Logger = new LoggerConfiguration().WriteTo.Debug().WriteTo.Console().CreateLogger();
        builder.Services.AddSerilog(Log.Logger);

        builder.UseWolverine(options =>
        {
            options.CodeGeneration.TypeLoadMode = JasperFx.CodeGeneration.TypeLoadMode.Static;
            options
                .Policies.OnAnyException()
                .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds());

            options
                .UseRabbitMq(factory =>
                {
                    factory.HostName = messaging.Host;
                    factory.Port = messaging.Port;
                    factory.UserName = messaging.Username;
                    factory.Password = messaging.Password;
                    factory.VirtualHost = messaging.VirtualHost;
                })
                .AutoProvision()
                .UseConventionalRouting();

            options.Policies.UseDurableOutboxOnAllSendingEndpoints();
            options.Policies.UseDurableInboxOnAllListeners();
            options.Policies.AutoApplyTransactions();
            options.EnvelopeIdGeneration = EnvelopeIdGeneration.GuidV7;

            foreach (var module in modules.Modules)
            {
                options.Discovery.IncludeAssembly(module.Assembly);
                module.ConfigureWolverine(options, builder.Configuration);
            }

            options
                .Services.AddMarten(store =>
                {
                    store.Connection(marten.ConnectionString);
                    store.DatabaseSchemaName = marten.SchemaName;
                    store.DisableNpgsqlLogging = true;

                    foreach (var module in modules.Modules)
                    {
                        module.ConfigureMarten(store);
                    }
                })
                .IntegrateWithWolverine(integration =>
                {
                    integration.MessageStorageSchemaName = marten.SchemaName;
                });

            options.UseFluentValidation();
        });

        return builder;
    }

    /// <summary>
    /// Adds the common middleware and maps health and Wolverine HTTP endpoints.
    /// </summary>
    public static WebApplication UseWBandFoundation(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks(
            "/health/live",
            new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("live") }
        );
        app.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("ready") }
        );
        app.MapWolverineEndpoints();

        return app;
    }

    private static void Copy(MessagingOptions source, MessagingOptions target)
    {
        target.Host = source.Host;
        target.Port = source.Port;
        target.Username = source.Username;
        target.Password = source.Password;
        target.VirtualHost = source.VirtualHost;
    }

    private static void Copy(MartenOptions source, MartenOptions target)
    {
        target.ConnectionString = source.ConnectionString;
        target.SchemaName = source.SchemaName;
    }
}
