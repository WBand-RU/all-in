using Auth;
using JasperFx;
using RabbitMQ.Client;
using Shared;
using WBand.Modules.FileModule;
using WBand.Modules.UserModule;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var assembly = typeof(Program).Assembly;
builder.AddShared(assembly);

AddAuthorization(builder);

AddEventDriven(builder, assembly);

builder.AddUserModule();
builder.AddFileModule();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
}

app.UseKeycloakAuthentication();

app.MapFileModuleEndpoints();

return await app.RunJasperFxCommands(args);

static void AddAuthorization(WebApplicationBuilder builder)
{
    var keycloakUrl =
        builder.Configuration["Keycloak:Url"] ?? EnvironmentVariable.Get("KEYCLOAK_URL");
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
}

static void AddEventDriven(WebApplicationBuilder builder, System.Reflection.Assembly assembly)
{
    var rabbitmqHost =
        builder.Configuration["RabbitMQ:Host"] ?? EnvironmentVariable.Get("MESSAGING_HOST");
    var rabbitmqPort = ushort.Parse(
        builder.Configuration["RabbitMQ:Port"] ?? EnvironmentVariable.Get("MESSAGING_PORT")
    );
    var rabbitmqUsername =
        builder.Configuration["RabbitMQ:Username"] ?? EnvironmentVariable.Get("MESSAGING_USERNAME");
    var rabbitmqPassword =
        builder.Configuration["RabbitMQ:Password"] ?? EnvironmentVariable.Get("MESSAGING_PASSWORD");
    var rabbitmqVirtualHost =
        builder.Configuration["RabbitMQ:VirtualHost"]
        ?? EnvironmentVariable.Get("MESSAGING_VIRTUAL_HOST");

    builder.AddEventDriven(
        rabbitmqHost,
        rabbitmqPort,
        rabbitmqUsername,
        rabbitmqPassword,
        rabbitmqVirtualHost,
        builder.Configuration["Marten:ConnectionString"]
            ?? EnvironmentVariable.Get("MARTEN_DATABASE_CONNECTION_STRING"),
        builder.Configuration["Marten:SchemaName"]
            ?? EnvironmentVariable.Get("MARTEN_DATABASE_SCHEMA_NAME"),
        assembly
    );
}
