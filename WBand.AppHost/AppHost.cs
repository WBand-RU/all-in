using Aspire.Hosting;
using Aspire.Hosting.Yarp.Transforms;

var builder = DistributedApplication.CreateBuilder(args);

//var cache = builder.AddRedis("cache")
//    .WithDataVolume("wband-cache")
//    .WithLifetime(ContainerLifetime.Persistent);

var mongo = builder
    .AddMongoDB("mongo")
    .WithDataVolume("wband-mongo")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithMongoExpress(x => x.WithLifetime(ContainerLifetime.Persistent));

var keycloakUsername = builder.AddParameter("username", "admin");
var keycloakPassword = builder.AddParameter("password", "admin", secret: true);

var keycloak = builder
    .AddKeycloak("keycloak", 8080, keycloakUsername, keycloakPassword)
    .WithDataVolume("wband-keycloak")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment(x =>
    {
        x.EnvironmentVariables.Add("KC_HOSTNAME_URL", "http://localhost/identity-api");
    })
    .WithRealmImport("./Realms");

var bandsDatabase = mongo.AddDatabase("bands");
var bandService = builder
    .AddProject<Projects.BandService>("band-service")
    .WithHttpHealthCheck("/health")
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithReference(bandsDatabase)
    .WaitFor(bandsDatabase)
    .WithEnvironment(x =>
    {
        x.EnvironmentVariables.Add("KEYCLOAK_CLIENT_ID", "band-api");
        x.EnvironmentVariables.Add("KEYCLOAK_CLIENT_SECRET", "jRufmyJbvAa91dgUNIRqGJYDQSsFB1ZT");
    });

var songsDatabase = mongo.AddDatabase("songs");
var songService = builder
    .AddProject<Projects.SongService>("song-service")
    .WithHttpHealthCheck("/health")
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithReference(songsDatabase)
    .WaitFor(songsDatabase)
    .WithEnvironment(x =>
    {
        x.EnvironmentVariables.Add("KEYCLOAK_CLIENT_ID", "song-api");
        x.EnvironmentVariables.Add("KEYCLOAK_CLIENT_SECRET", "jRufmyJbvAa91dgUNIRqGJYDQSsFB1ZT");
    });

// var equipmentDatabase = mongo.AddDatabase("equipment");
// var equipmentService = builder
//     .AddProject<Projects.EquipmentService>("equipment-service")
//     //.WithHttpHealthCheck("/health")
//     .WithReference(keycloak)
//     .WaitFor(keycloak)
//     .WithReference(equipmentDatabase)
//     .WaitFor(equipmentDatabase)
//     .WithEnvironment(x =>
//     {
//         x.EnvironmentVariables.Add("KEYCLOAK_CLIENT_ID", "equipment-api");
//         x.EnvironmentVariables.Add("KEYCLOAK_CLIENT_SECRET", "XOLFxXQpUAEYetK0dbCdTfeGcl2QsxIw");
//     });

var web = builder
    .AddViteApp("web", packageManager: "pnpm")
    .WithExternalHttpEndpoints()
    .WithPnpmPackageInstallation(x =>
    {
        x.WithIconName("WindowConsole");
    })
    .WithHttpHealthCheck("/health")
    .WithReference(songService)
    .WaitFor(songService);

builder
    .AddPnpmApp("api-generator", "../web", "generate-api")
    .WithParentRelationship(web)
    .WaitFor(songService)
    .WithIconName("CodeBlockEdit");

builder
    .AddYarp("proxy")
    .WithEndpoint("http", endpoint => endpoint.Port = 80)
    .WithConfiguration(x =>
    {
        x.AddRoute("/band-api/{**catch-all}", bandService)
            .WithTransformPathRemovePrefix("/band-api");

        x.AddRoute("/song-api/{**catch-all}", songService)
            .WithTransformPathRemovePrefix("/song-api");

        x.AddRoute("/identity-api/{**catch-all}", keycloak)
            .WithTransformPathRemovePrefix("/identity-api")
            .WithTransformRequestHeadersAllowed(
                "X-Forwarder-For",
                "X-Forwarder-Proto",
                "X-Forwarder-Host",
                "Host"
            );

        x.AddRoute(web);
    });

builder.Build().Run();
