using WBand.Modules.SongModule;
using WBand.Modules.BandModule;
using FluentValidation;
using JasperFx;
using JasperFx.Core;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using WBand.WebAPI.Configuration;
using WBand.WebAPI.Data;
using WBand.Modules.BandModule.Contracts;
using WBand.WebAPI.HealthChecks;
using WBand.WebAPI.Services;
using WBand.WebAPI.Services.Implementations;
using WBand.WebAPI.Shared;
using Wolverine;
using WBand.Modules.EventModule;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.RabbitMQ;

try
{
    var builder = WebApplication.CreateBuilder(args);

    WebApiConfiguration configuration = ConfigureApplication(builder);

    builder.AddWBandApiAuthentication(configuration);

    AddWBandFoundation(builder, configuration);

    builder.Services.AddApplicationPersistence(configuration.DatabaseConnectionString);

    builder.Services.AddEventModule(configuration.DatabaseConnectionString);

    var app = builder.Build();

    await DatabaseSetup.ApplyMigrations(configuration.DatabaseConnectionString, default);

    await app.Services.MigrateEvents();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().AllowAnonymous();
    }

    app.MapHealthChecks("/health").AllowAnonymous();

    UseWBandFoundation(app);

    return await app.RunJasperFxCommands(args);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Fatal Program");
    await Log.CloseAndFlushAsync();

    return -1;
}

static WebApiConfiguration ConfigureApplication(WebApplicationBuilder builder)
{
    builder
        .Services.AddOptions<WebApiConfiguration>()
        .Bind(builder.Configuration)
        .ValidateDataAnnotations()
        .ValidateOnStart();
    WebApiConfiguration configuration =
        builder.Configuration.Get<WebApiConfiguration>()
        ?? throw new InvalidProgramException($"{nameof(WebApiConfiguration)} is not set");
    return configuration;
}

static WebApplicationBuilder AddWBandFoundation(
    WebApplicationBuilder builder,
    WebApiConfiguration configuration
)
{
    builder.Services.AddProblemDetails();
    builder.Services.AddOpenApi();
    builder
        .Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
        .AddCheck(
            "postgresql",
            new PostgreSqlHealthCheck(configuration.DatabaseConnectionString),
            tags: ["ready"]
        )
        .AddCheck(
            "rabbitmq",
            new TcpEndpointHealthCheck(
                configuration.RABBITMQ_HOST,
                int.Parse(configuration.RABBITMQ_PORT)
            ),
            tags: ["ready"]
        );
    builder.Services.AddWolverineHttp();
    // These validators contain only immutable rules and no request-scoped dependencies.
    // Wolverine resolves the validator collection while building its cached HTTP handlers.
    builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly, ServiceLifetime.Singleton);

    Log.Logger = new LoggerConfiguration().WriteTo.Debug().WriteTo.Console().CreateLogger();
    builder.Services.AddSerilog(Log.Logger);

    builder.UseWolverine(options =>
    {
        // Static mode requires generated handlers to be compiled into every module assembly.
        // Dynamic mode is required during normal development when endpoints are added or changed.
        options.CodeGeneration.TypeLoadMode = JasperFx.CodeGeneration.TypeLoadMode.Dynamic;
        options
            .Policies.OnAnyException()
            .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds());

        // MVP domain writes are synchronous. Only the Keycloak incoming broker is configured.
        options.EnvelopeIdGeneration = EnvelopeIdGeneration.GuidV7;

        options.Discovery.IncludeAssembly(typeof(Program).Assembly);
        BandModule.Configure(options);
        SongModule.Configure(options);
        options.Discovery.IncludeAssembly(typeof(EventModule).Assembly);

        ConfigureWolverine();
        void ConfigureWolverine()
        {
            BrokerName keycloakBroker = new(configuration.KEYCLOAK_EVENTS_EXCHANGE);
            var broker = options.AddNamedRabbitMqBroker(
                keycloakBroker,
                factory =>
                {
                    factory.HostName = configuration.RABBITMQ_HOST;
                    factory.Port = int.Parse(configuration.RABBITMQ_PORT);
                    factory.UserName = configuration.RABBITMQ_USERNAME;
                    factory.Password = configuration.RABBITMQ_PASSWORD;
                    factory.VirtualHost = configuration.RABBITMQ_VIRTUAL_HOST;
                    factory.Ssl.Enabled = configuration.RABBITMQ_SSL_ENABLED.Equals(
                        "true",
                        StringComparison.InvariantCultureIgnoreCase
                    );
                }
            );

            string routingKey = $"KK.EVENT.CLIENT.{configuration.KEYCLOAK_REALM}.SUCCESS.#";

            broker
                .AutoProvision()
                .BindExchange(configuration.KEYCLOAK_EVENTS_EXCHANGE, ExchangeType.Topic)
                .ToQueue(configuration.KEYCLOAK_EVENTS_QUEUE, routingKey);

            options
                .ListenToRabbitQueueOnNamedBroker(
                    keycloakBroker,
                    configuration.KEYCLOAK_EVENTS_QUEUE
                )
                .DefaultIncomingMessage<KeycloakEvent>()
                .ProcessInline();
        }

        options.UseFluentValidation();
    });

    return builder;
}

static WebApplication UseWBandFoundation(WebApplication app)
{
    app.UseExceptionHandler();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks(
            "/health/live",
            new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("live"),
            }
        )
        .AllowAnonymous();

    app.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("ready"),
            }
        )
        .AllowAnonymous();

    app.MapWolverineEndpoints(options =>
    {
        options.UseFluentValidationProblemDetailMiddleware();
    });

    return app;
}

/// <summary>
/// Exposes the application entry point to integration tests.
/// </summary>
public partial class Program;
