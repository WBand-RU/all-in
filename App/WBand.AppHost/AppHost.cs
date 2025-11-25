using Aspire.Hosting;
using Aspire.Hosting.Yarp.Transforms;
using WBand.ServiceDefaults;

var builder = DistributedApplication.CreateBuilder(args);

const string MESSAGE_QUEUE_DEFAULT_USERNAME = "guest";

var messageQueue = builder
    .AddRabbitMQ("messaging")
    .WithManagementPlugin(15672)
    .WithDataVolume("wband-rabbitmq")
    .WithLifetime(ContainerLifetime.Persistent);

var mongo = builder
    .AddMongoDB("mongo")
    .WithDataVolume("wband-mongo")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithMongoExpress(x => x.WithLifetime(ContainerLifetime.Persistent).WithHostPort(5050));

// MinIO for file storage
var minioUsername = builder.AddParameter("minio-username", "minioadmin");
var minioPassword = builder.AddParameter("minio-password", "minioadmin", secret: true);

var minio = builder
    .AddMinioContainer("minio", minioUsername, minioPassword, 9000)
    .WithBindMount("wband-minio", "/data")
    .WithLifetime(ContainerLifetime.Persistent);

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
    .WithReference(messageQueue)
    .WaitFor(messageQueue)
    .WithEnvironment(async x =>
    {
        AddKeycloackEnv(x);
        await AddMessageQueueEnv(x, MESSAGE_QUEUE_DEFAULT_USERNAME, messageQueue);
    });

var songsDatabase = mongo.AddDatabase("songs");
var songService = builder
    .AddProject<Projects.SongService>("song-service")
    .WithHttpHealthCheck("/health")
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithReference(songsDatabase)
    .WaitFor(songsDatabase)
    .WithReference(messageQueue)
    .WaitFor(messageQueue)
    .WithEnvironment(async x =>
    {
        AddKeycloackEnv(x);
        await AddMessageQueueEnv(x, MESSAGE_QUEUE_DEFAULT_USERNAME, messageQueue);
    });

var playlistsDatabase = mongo.AddDatabase("playlists");
var playlistService = builder
    .AddProject<Projects.PlaylistService>("playlist-service")
    .WithHttpHealthCheck("/health")
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithReference(playlistsDatabase)
    .WaitFor(playlistsDatabase)
    .WithEnvironment(async x =>
    {
        AddKeycloackEnv(x);
        await AddMessageQueueEnv(x, MESSAGE_QUEUE_DEFAULT_USERNAME, messageQueue);
    });

var playbacksDatabase = mongo.AddDatabase("playbacks");
var playbackService = builder
    .AddProject<Projects.PlaybackService>("playback-service")
    .WithHttpHealthCheck("/health")
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithReference(playbacksDatabase)
    .WaitFor(playbacksDatabase)
    .WithReference(minio)
    .WaitFor(minio)
    .WithReference(messageQueue)
    .WaitFor(messageQueue)
    .WithEnvironment(async x =>
    {
        AddKeycloackEnv(x);
        await AddMessageQueueEnv(x, MESSAGE_QUEUE_DEFAULT_USERNAME, messageQueue);
    });

var chatsDatabase = mongo.AddDatabase("chats");
var chatService = builder
    .AddProject<Projects.ChatService>("chat-service")
    .WithHttpHealthCheck("/health")
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithReference(chatsDatabase)
    .WaitFor(chatsDatabase)
    .WithEnvironment(async x =>
    {
        AddKeycloackEnv(x);
        await AddMessageQueueEnv(x, MESSAGE_QUEUE_DEFAULT_USERNAME, messageQueue);
    });

var notificationsDatabase = mongo.AddDatabase("notifications");
var notificationService = builder
    .AddProject<Projects.NotificationService>("notification-service")
    .WithHttpHealthCheck("/health")
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithReference(notificationsDatabase)
    .WaitFor(notificationsDatabase)
    .WithEnvironment(async x =>
    {
        AddKeycloackEnv(x);
        await AddMessageQueueEnv(x, MESSAGE_QUEUE_DEFAULT_USERNAME, messageQueue);
    });

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

        x.AddRoute("/playlist-api/{**catch-all}", playlistService)
            .WithTransformPathRemovePrefix("/playlist-api");

        x.AddRoute("/playback-api/{**catch-all}", playbackService)
            .WithTransformPathRemovePrefix("/playback-api");

        x.AddRoute("/chat-api/{**catch-all}", chatService)
            .WithTransformPathRemovePrefix("/chat-api");

        x.AddRoute("/notification-api/{**catch-all}", notificationService)
            .WithTransformPathRemovePrefix("/notification-api");

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

static void AddKeycloackEnv(EnvironmentCallbackContext x)
{
    x.EnvironmentVariables.Add("KEYCLOAK_CLIENT_ID", "band-api");
    x.EnvironmentVariables.Add("KEYCLOAK_CLIENT_SECRET", "jRufmyJbvAa91dgUNIRqGJYDQSsFB1ZT");
}

static async Task AddMessageQueueEnv(
    EnvironmentCallbackContext x,
    string MESSAGE_QUEUE_DEFAULT_USERNAME,
    IResourceBuilder<RabbitMQServerResource> messageQueue
)
{
    x.EnvironmentVariables[EnvironmentVariablesConstants.MessageQueueUrl] = messageQueue
        .Resource
        .PrimaryEndpoint
        .Host;
    x.EnvironmentVariables[EnvironmentVariablesConstants.MessageQueuePort] =
        messageQueue.Resource.PrimaryEndpoint.Port.ToString();
    x.EnvironmentVariables[EnvironmentVariablesConstants.MessageQueueUsername] =
        MESSAGE_QUEUE_DEFAULT_USERNAME;
    x.EnvironmentVariables[EnvironmentVariablesConstants.MessageQueuePassword] =
        (await messageQueue.Resource.PasswordParameter.GetValueAsync(x.CancellationToken))
        ?? throw new InvalidOperationException("Password for message queue not found");
}
