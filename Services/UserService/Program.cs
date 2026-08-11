using Auth;
using JasperFx;
using Shared;
using UserService.Services;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

var keycloakUrl = builder.Configuration["Keycloak:Url"] ?? EnvironmentVariable.Get("KEYCLOAK_URL");
var keycloakRealm =
    builder.Configuration["Keycloak:Realm"] ?? EnvironmentVariable.Get("KEYCLOAK_REALM");
var keycloakClientId =
    builder.Configuration["Keycloak:ClientId"] ?? EnvironmentVariable.Get("KEYCLOAK_CLIENT_ID");
var keycloakClientSecret =
    builder.Configuration["Keycloak:ClientSecret"]
    ?? EnvironmentVariable.Get("KEYCLOAK_CLIENT_SECRET");
var keycloakSslRequired = bool.Parse(
    builder.Configuration["Keycloak:SslRequired"]
        ?? EnvironmentVariable.Get("KEYCLOAK_SSL_REQUIRED")
);

builder.AddKeycloakAuthentication(
    keycloakUrl,
    keycloakRealm,
    keycloakClientId,
    keycloakClientSecret,
    keycloakSslRequired
);

var assembly = typeof(Program).Assembly;
builder.AddEventDriven(
    builder.Configuration["RabbitMQ:Host"] ?? EnvironmentVariable.Get("MESSAGING_HOST"),
    ushort.Parse(
        builder.Configuration["RabbitMQ:Port"] ?? EnvironmentVariable.Get("MESSAGING_PORT")
    ),
    builder.Configuration["RabbitMQ:Username"] ?? EnvironmentVariable.Get("MESSAGING_USERNAME"),
    builder.Configuration["RabbitMQ:Password"] ?? EnvironmentVariable.Get("MESSAGING_PASSWORD"),
    builder.Configuration["Marten:ConnectionString"]
        ?? EnvironmentVariable.Get("MARTEN_DATABASE_CONNECTION_STRING"),
    builder.Configuration["Marten:SchemaName"]
        ?? EnvironmentVariable.Get("MARTEN_DATABASE_SCHEMA_NAME"),
    assembly
);

builder.AddShared(assembly);

builder.Services.AddWolverineHttp();

builder.AddRabbitMQClient(connectionName: "messaging");
builder.Services.AddHostedService<KeycloakMessagesReader>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
}

app.UseKeycloakAuthentication();

app.UseWolverineEndpoints();

await app.RunJasperFxCommands(args);
