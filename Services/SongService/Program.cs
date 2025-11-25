using Auth;
using MessagesCommon;
using Shared;
using SongService.Configuration;
using SongService.Endpoints;
using SongService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppSettings>(builder.Configuration);
var appSettings =
    builder.Configuration.Get<AppSettings>()
    ?? throw new InvalidOperationException("AppSettings not found");

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.AddMongoDBClient(connectionName: "songs");

builder.AddKeycloakAuthentication();

builder.AddMessaging(
    appSettings.MessageQueueUrl,
    int.Parse(appSettings.MessageQueuePort),
    appSettings.MessageQueueUsername,
    appSettings.MessageQueuePassword
);

var assembly = typeof(Program).Assembly;
builder.AddShared(assembly);

builder.Services.AddScoped<Repository>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapEndpoints();

await app.RunAsync();
