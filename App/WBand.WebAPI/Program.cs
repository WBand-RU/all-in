using Auth;
using JasperFx;
using Shared;
using Shared.Modules;
using WBand.Modules.FileModule;
using WBand.Modules.BandModule;
using WBand.Modules.UserModule;
using WBand.Modules.SongModule;

var builder = WebApplication.CreateBuilder(args).ApplyWBandConfiguration();

var modules = new ModuleCatalog(new UserModule(), new BandModule(), new SongModule(), new FileModule());
modules.AddServices(builder);

builder.AddKeycloakAuthentication();
builder.AddWBandFoundation(modules);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseWBandFoundation();
modules.MapEndpoints(app);

return await app.RunJasperFxCommands(args);

/// <summary>
/// Exposes the application entry point to integration tests.
/// </summary>
public partial class Program;
