using Auth;
using JasperFx;
using Shared;
using UserService.Services;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.AddKeycloakAuthentication();

var assembly = typeof(Program).Assembly;
builder.AddEventDriven(
    EnvironmentVariable.Get("MESSAGING_HOST"),
    ushort.Parse(EnvironmentVariable.Get("MESSAGING_PORT")),
    EnvironmentVariable.Get("MESSAGING_USERNAME"),
    EnvironmentVariable.Get("MESSAGING_PASSWORD"),
    EnvironmentVariable.Get("USERS_DATABASE_URI"),
    EnvironmentVariable.Get("MARTEN_DATABASE_SCHEMA_NAME"),
    assembly
);

builder.AddShared(assembly);

builder.Services.AddWolverineHttp();

builder.AddRabbitMQClient(connectionName: "messaging");
builder.Services.AddHostedService<KeycloakMessagesReader>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
}

app.UseWolverineEndpoints();

await app.RunJasperFxCommands(args);
