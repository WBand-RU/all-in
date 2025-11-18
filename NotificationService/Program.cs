using Auth;

using NotificationService.Endpoints;
using NotificationService.Hubs;
using NotificationService.Services;

using Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddCors();

builder.AddMongoDBClient(connectionName: "notifications");

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

app.UseCors(policy =>
{
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/notification-hub");
app.AddEndpoints();

app.Run();
