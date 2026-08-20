using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WBand.Dev.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Services.AddHttpClient("health");
builder.Services.AddSingleton<ServiceInfoManager>();
builder.Services.AddSingleton<ProcessManager>();
builder.Services.AddScoped<HealthChecker>();
builder.Services.AddHostedService<AppHost>();
builder.Services.AddHostedService<ConsoleDashboard>();

var app = builder.Build();

app.AddDotnet("Web API", "./App/WBand.WebAPI/", "http://localhost:5000/health")
    .AddDotnet("Mixer Worker", "./App/WBand.MixerWorker/")
    .AddBun("Generate WebApi Client", "./App/web", "generate-api")
    .AddBun("Frontend", "./App/web", "dev", "http://localhost:3000/health");

await app.RunAsync();
