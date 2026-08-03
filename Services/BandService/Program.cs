using Auth;
using BandService.Services;
using Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.AddKeycloakAuthentication();

builder.AddEventDriven(
    EnvironmentVariable.Get("MESSAGING_HOST"),
    ushort.Parse(EnvironmentVariable.Get("MESSAGING_PORT")),
    EnvironmentVariable.Get("MESSAGING_USERNAME"),
    EnvironmentVariable.Get("MESSAGING_PASSWORD"),
    EnvironmentVariable.Get("POSTGRESDB_URI"),
    EnvironmentVariable.Get("MARTEN_DATABASE_SCHEMA_NAME"),
    typeof(Program).Assembly
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
    _ = app.MapOpenApi();
}

app.UseWolverineEndpoints();

await app.RunAsync();
