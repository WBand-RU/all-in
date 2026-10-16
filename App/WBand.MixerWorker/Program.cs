using JasperFx;
using Marten;
using Quartz;
using Shared.Configuration;
using WBand.MixerWorker;
using WBand.Modules.FileModule;
using WBand.Modules.MixerModule;
using WBand.Modules.MixerModule.Domain;
using WBand.Modules.StemModule;
using Weasel.Core;
using Wolverine;
using Wolverine.Marten;

var builder = Host.CreateApplicationBuilder(args);
var fileModule = new FileModule();
var stemModule = new StemModule();
var mixerModule = new MixerModule();

FileModule.AddStorageServices(builder, addUploadCleanup: false);
stemModule.AddServices(builder);
mixerModule.AddServices(builder);

var marten = MartenOptions.FromConfiguration(builder.Configuration);
builder.UseWolverine(options =>
{
    // Quartz owns the durable mix queue and leases. Wolverine is only used for
    // synchronous module calls here, so this host must not run messaging agents.
    options.Durability.Mode = DurabilityMode.MediatorOnly;
    options.CodeGeneration.TypeLoadMode = JasperFx.CodeGeneration.TypeLoadMode.Dynamic;
    options.Discovery.IncludeAssembly(fileModule.Assembly);
    options.Discovery.IncludeAssembly(stemModule.Assembly);
    options.Discovery.IncludeAssembly(mixerModule.Assembly);
    options.Services.AddMarten(store =>
    {
        store.Connection(marten.ConnectionString);
        store.DatabaseSchemaName = marten.SchemaName;
        store.DisableNpgsqlLogging = true;
        store.UseSystemTextJsonForSerialization(EnumStorage.AsString);
        fileModule.ConfigureMarten(store);
        stemModule.ConfigureMarten(store);
        mixerModule.ConfigureMarten(store);
    }).IntegrateWithWolverine(integration =>
    {
        integration.MessageStorageSchemaName = marten.SchemaName;
    });
});

builder.Services.AddQuartz(configuration =>
{
    var jobKey = new JobKey(nameof(GenerateQueuedMixesJob));
    configuration.AddJob<GenerateQueuedMixesJob>(options => options.WithIdentity(jobKey));
    configuration.AddTrigger(options => options.ForJob(jobKey)
        .WithIdentity("mix-queue-poll")
        .StartNow()
        .WithSimpleSchedule(schedule => schedule.WithIntervalInSeconds(5).RepeatForever()));
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

await builder.Build().RunAsync();
