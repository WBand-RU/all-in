using System.Reflection;
using Auth;
using MassTransit;
using MessagesCommon;
using Minio;
using PlaybackService.Configuration;
using PlaybackService.Endpoints;
using PlaybackService.Services;
using Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppSettings>(builder.Configuration);
var appSettings =
    builder.Configuration.Get<AppSettings>()
    ?? throw new InvalidOperationException("AppSettings not found");

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.AddMongoDBClient(connectionName: "playbacks");

builder.AddKeycloakAuthentication();

// Add MinIO client
builder.AddMinioClient("minio");
builder.Services.AddScoped<MinioStorageService>();

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
