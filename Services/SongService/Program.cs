using Auth;
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

//builder.AddMongoDBClient(connectionName: "songs");

builder.AddKeycloakAuthentication();

builder.AddEventDriven(
    appSettings.MessageQueueHost,
    ushort.Parse(appSettings.MessageQueuePort),
    appSettings.MessageQueueUsername,
    appSettings.MessageQueuePassword,
    appSettings.MartenDatabaseConnectionString,
    appSettings.MartenDatabaseSchemaName,
    [typeof(Program).Assembly]
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
