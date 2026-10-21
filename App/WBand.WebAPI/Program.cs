using System.Security.Claims;
using FluentValidation;
using JasperFx;
using JasperFx.Core;
using Keycloak.AuthServices.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.SongModule.Contracts;
using WBand.Modules.UserModule.Events;
using WBand.WebAPI.Configuration;
using WBand.WebAPI.Data;
using WBand.WebAPI.Events;
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

    AddKeycloakAuthentication(builder, configuration);

    AddWBandFoundation(builder, configuration);

    builder.Services.AddDatabase(configuration.DatabaseConnectionString);

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

static IHostApplicationBuilder AddKeycloakAuthentication(
    IHostApplicationBuilder builder,
    WebApiConfiguration configuration
)
{
    builder.Services.AddKeycloakWebApiAuthentication(
        options =>
        {
            options.AuthServerUrl = configuration.KEYCLOAK_URI;
            options.Realm = configuration.KEYCLOAK_REALM;
            options.Resource = configuration.KEYCLOAK_CLIENT_ID;
            options.VerifyTokenAudience = true;
            options.Credentials.Secret = configuration.KEYCLOAK_CLIENT_SECRET;
            options.SslRequired = configuration.KEYCLOAK_SSL_REQUIRED.Equals(
                "true",
                StringComparison.CurrentCultureIgnoreCase
            )
                ? "external"
                : "none";
        },
        options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters.ValidateIssuer = true;
            options.TokenValidationParameters.ValidateAudience = true;
            options.TokenValidationParameters.ValidateLifetime = true;
            options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
            options.Events.OnTokenValidated = context =>
            {
                var subject =
                    context.Principal?.FindFirstValue("sub")
                    ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var email =
                    context.Principal?.FindFirstValue("email")
                    ?? context.Principal?.FindFirstValue(ClaimTypes.Email);
                if (!Guid.TryParse(subject, out _) || string.IsNullOrWhiteSpace(email))
                    context.Fail(
                        "The access token must identify a WBand user and contain an email."
                    );
                return Task.CompletedTask;
            };
        }
    );

    builder
        .Services.AddAuthorization(options =>
        {
            options.FallbackPolicy =
                new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            options.AddPolicy(Roles.SuperAdmin, policy => policy.RequireRole(Roles.SuperAdmin));
            options.AddPolicy(
                Roles.Moderator,
                policy => policy.RequireRole(Roles.SuperAdmin, Roles.Moderator)
            );
        })
        .AddKeycloakAuthorization(options =>
        {
            options.EnableRolesMapping = RolesClaimTransformationSource.Realm;
            options.AuthServerUrl = configuration.KEYCLOAK_URI;
            options.Realm = configuration.KEYCLOAK_REALM;
            options.Resource = configuration.KEYCLOAK_CLIENT_ID;
            options.Credentials.Secret = configuration.KEYCLOAK_CLIENT_SECRET;
            options.SslRequired = configuration.KEYCLOAK_SSL_REQUIRED.Equals(
                "true",
                StringComparison.InvariantCultureIgnoreCase
            )
                ? "external"
                : "none";
        });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, CurrentUser>();

    return builder;
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

        options
            .UseRabbitMq(factory =>
            {
                factory.HostName = configuration.RABBITMQ_HOST;
                factory.Port = int.Parse(configuration.RABBITMQ_PORT);
                factory.UserName = configuration.RABBITMQ_USERNAME;
                factory.Password = configuration.RABBITMQ_PASSWORD;
                factory.VirtualHost = configuration.RABBITMQ_VIRTUAL_HOST;
            })
            .ConfigureChannelCreation(channel =>
            {
                channel.PublisherConfirmationsEnabled = true;
                channel.PublisherConfirmationTrackingEnabled = true;
            })
            .AutoProvision()
            .UseConventionalRouting();

        // Publish directly after committing EF Core transactions. Inline sends await broker confirms.
        options.PublishMessage<UserCreated>().ToRabbitExchange("user-created").SendInline();
        options.PublishMessage<UserLoggedIn>().ToRabbitExchange("user-logged-in").SendInline();
        options.PublishMessage<BandCreated>().ToRabbitExchange("band-created").SendInline();
        options.PublishMessage<MemberInvited>().ToRabbitExchange("member-invited").SendInline();
        options
            .PublishMessage<MembershipChanged>()
            .ToRabbitExchange("membership-changed")
            .SendInline();
        options.PublishMessage<SongCreated>().ToRabbitExchange("song-created").SendInline();
        options.PublishMessage<SongChanged>().ToRabbitExchange("song-changed").SendInline();
        options.PublishMessage<SongDeleted>().ToRabbitExchange("song-deleted").SendInline();
        options.PublishMessage<SongRestored>().ToRabbitExchange("song-restored").SendInline();
        options
            .PublishMessage<SongPartitionsChanged>()
            .ToRabbitExchange("song-partitions-changed")
            .SendInline();
        options.PublishMessage<SongSectionsChanged>().ToRabbitExchange("song-sections-changed").SendInline();
        options.PublishMessage<SongContentChanged>().ToRabbitExchange("song-content-changed").SendInline();
        options.EnvelopeIdGeneration = EnvelopeIdGeneration.GuidV7;

        options.Discovery.IncludeAssembly(typeof(Program).Assembly);
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
