using System.Reflection;
using Auth;
using Minio;
using PlaybackService.Endpoints;
using PlaybackService.Services;
using Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.AddMongoDBClient(connectionName: "playbacks");

builder.AddKeycloakAuthentication();

// Add MinIO client
builder.AddMinioClient("minio");
builder.Services.AddScoped<MinioStorageService>();

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
