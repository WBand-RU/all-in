using Auth;
using BandService.Configuration;
using BandService.Endpoints;
using BandService.Services;
using MessagesCommon;
using Shared;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppSettings>(builder.Configuration);
var appSettings =
    builder.Configuration.Get<AppSettings>()
    ?? throw new InvalidOperationException("AppSettings not found");

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.AddMongoDBClient(connectionName: "bands");

builder.AddKeycloakAuthentication();

// Configure MassTransit
builder.AddMessaging(
    appSettings.MessageQueueUrl,
    int.Parse(appSettings.MessageQueuePort),
    appSettings.MessageQueueUsername,
    appSettings.MessageQueuePassword
);

var assembly = typeof(Program).Assembly;
builder.AddShared(assembly);

builder.Services.AddScoped<Repository>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<KeycloakAdminClient>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapEndpoints();

await app.RunAsync();
