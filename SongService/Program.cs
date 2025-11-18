using System.Reflection;
using Auth;
using Shared;
using SongService.Endpoints;
using SongService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.AddMongoDBClient(connectionName: "songs");

builder.AddKeycloakAuthentication();

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
