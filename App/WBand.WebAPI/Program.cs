using Auth;
using JasperFx;
using Shared;
using Shared.Modules;
using WBand.Modules.BandModule;
using WBand.Modules.FileModule;
using WBand.Modules.PlaylistModule;
using WBand.Modules.SongModule;
using WBand.Modules.StemModule;
using WBand.Modules.MixerModule;
using WBand.Modules.UserModule;

var builder = WebApplication.CreateBuilder(args).ApplyWBandConfiguration();

var modules = new ModuleCatalog(
    new UserModule(),
    new BandModule(),
    new SongModule(),
    new PlaylistModule(),
    new FileModule(),
    new StemModule(),
    new MixerModule()
);
modules.AddServices(builder);

builder.AddKeycloakAuthentication();
builder.AddWBandFoundation(modules);

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.MapHealthChecks("/health").AllowAnonymous();

app.UseWBandFoundation();
modules.MapEndpoints(app);

return await app.RunJasperFxCommands(args);

/// <summary>
/// Exposes the application entry point to integration tests.
/// </summary>
public partial class Program;
