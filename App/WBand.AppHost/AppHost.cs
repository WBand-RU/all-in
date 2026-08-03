using Aspire.Hosting;
using Aspire.Hosting.Yarp.Transforms;
using Projects;
using WBand.ServiceDefaults;

var builder = DistributedApplication.CreateBuilder(args);

const string minioAccessKey = "maB5Gw8pepE6J3oXrZWB";
const string minioSecretKey = "i4mFPgikUqWSmoU1JtGg1T3BP54hHDism37AHGA0";

var messaging = AddMessaging(builder);
var postgres = AddPostgres(builder);
var keycloak = await AddKeycloak(builder, messaging);
var minio = AddMinio(builder);

var userDatabase = postgres.AddDatabase("users-database");
var userService = builder
    .AddProject<UserService>("user-service")
    .WithHttpHealthCheck("/health")
    .WithReference(userDatabase)
    .WaitFor(userDatabase)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("MARTEN_DATABASE_SCHEMA_NAME", "marten");

//var bandsDatabase = postgres.AddDatabase("bands");
//var bandService = builder
//    .AddProject<Projects.BandService>("band-service")
//    .WithHttpHealthCheck("/health")
//    .WithReference(keycloak)
//    .WaitFor(keycloak)
//    .WithReference(bandsDatabase)
//    .WaitFor(bandsDatabase)
//    .WithReference(messaging)
//    .WaitFor(messaging)
//    .WithEnvironment("MARTEN_DATABASE_SCHEMA_NAME", "marten");

//.WithEnvironment(async x =>
//{
//    AddKeycloackEnv(x);
//    await AddMessageQueueEnv(x, MESSAGE_QUEUE_DEFAULT_USERNAME, messaging);
//});

//var songsDatabase = postgres.AddDatabase("songs");
//var songService = builder
//    .AddProject<Projects.SongService>("song-service")
//    .WithHttpHealthCheck("/health")
//    .WithReference(keycloak)
//    .WaitFor(keycloak)
//    .WithReference(songsDatabase)
//    .WaitFor(songsDatabase)
//    .WithReference(messaging)
//    .WaitFor(messaging);

////.WithEnvironment(async x =>
////{
////    AddKeycloackEnv(x);
////    await AddMessageQueueEnv(x, MESSAGE_QUEUE_DEFAULT_USERNAME, messaging);
////});

//var playlistsDatabase = postgres.AddDatabase("playlists");
//var playlistService = builder
//    .AddProject<Projects.PlaylistService>("playlist-service")
//    .WithHttpHealthCheck("/health")
//    .WithReference(keycloak)
//    .WaitFor(keycloak)
//    .WithReference(playlistsDatabase)
//    .WaitFor(playlistsDatabase);

////.WithEnvironment(async x =>
////{
////    AddKeycloackEnv(x);
////    await AddMessageQueueEnv(x, MESSAGE_QUEUE_DEFAULT_USERNAME, messaging);
////});

//var playbacksDatabase = postgres.AddDatabase("playbacks");
//var playbackService = builder
//    .AddProject<Projects.PlaybackService>("playback-service")
//    .WithHttpHealthCheck("/health")
//    .WithReference(keycloak)
//    .WaitFor(keycloak)
//    .WithReference(playbacksDatabase)
//    .WaitFor(playbacksDatabase)
//    .WithReference(minio)
//    .WaitFor(minio)
//    .WithReference(messaging)
//    .WaitFor(messaging);

////.WithEnvironment(async x =>
////{
////    AddKeycloackEnv(x);
////    await AddMessageQueueEnv(x, MESSAGE_QUEUE_DEFAULT_USERNAME, messaging);
////});

//var chatsDatabase = postgres.AddDatabase("chats");
//var chatService = builder
//    .AddProject<Projects.ChatService>("chat-service")
//    .WithHttpHealthCheck("/health")
//    .WithReference(keycloak)
//    .WaitFor(keycloak)
//    .WithReference(chatsDatabase)
//    .WaitFor(chatsDatabase);

////.WithEnvironment(async x =>
////{
////    AddKeycloackEnv(x);
////    await AddMessageQueueEnv(x, MESSAGE_QUEUE_DEFAULT_USERNAME, messaging);
////});

//var notificationsDatabase = postgres.AddDatabase("notifications");
//var notificationService = builder
//    .AddProject<Projects.NotificationService>("notification-service")
//    .WithHttpHealthCheck("/health")
//    .WithReference(keycloak)
//    .WaitFor(keycloak)
//    .WithReference(notificationsDatabase)
//    .WaitFor(notificationsDatabase);

////.WithEnvironment(async x =>
////{
////    AddKeycloackEnv(x);
////    await AddMessageQueueEnv(x, MESSAGE_QUEUE_DEFAULT_USERNAME, messaging);
////});

var web = builder
    .AddViteApp("web", "../web")
    .WithBun()
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

//.WithReference(songService)
//.WaitFor(songService);

builder
    .AddViteApp("api-generator", "../web", "generate-api")
    .WithBun(false)
    .WithParentRelationship(web)
    //.WaitFor(userService)
    //.WaitFor(bandService)
    //.WaitFor(chatService)
    //.WaitFor(notificationService)
    //.WaitFor(playbackService)
    //.WaitFor(playlistService)
    //.WaitFor(songService)
    .WithIconName("CodeBlockEdit");

builder
    .AddYarp("proxy")
    .WithEndpoint("http", endpoint => endpoint.Port = 80)
    .WithConfiguration(x =>
    {
        //x.AddRoute("/user-api/{**catch-all}", userService)
        //    .WithTransformPathRemovePrefix("/user-api");

        //x.AddRoute("/band-api/{**catch-all}", bandService)
        //    .WithTransformPathRemovePrefix("/band-api");

        //x.AddRoute("/song-api/{**catch-all}", songService)
        //    .WithTransformPathRemovePrefix("/song-api");

        //x.AddRoute("/playlist-api/{**catch-all}", playlistService)
        //    .WithTransformPathRemovePrefix("/playlist-api");

        //x.AddRoute("/playback-api/{**catch-all}", playbackService)
        //    .WithTransformPathRemovePrefix("/playback-api");

        //x.AddRoute("/chat-api/{**catch-all}", chatService)
        //    .WithTransformPathRemovePrefix("/chat-api");

        //x.AddRoute("/notification-api/{**catch-all}", notificationService)
        //    .WithTransformPathRemovePrefix("/notification-api");

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

//static void AddKeycloackEnv(EnvironmentCallbackContext x)
//{
//    x.EnvironmentVariables.Add("KEYCLOAK_CLIENT_ID", "band-api");
//    x.EnvironmentVariables.Add("KEYCLOAK_CLIENT_SECRET", "jRufmyJbvAa91dgUNIRqGJYDQSsFB1ZT");
//}

//static async Task AddMessageQueueEnv(
//    EnvironmentCallbackContext x,
//    string MESSAGE_QUEUE_DEFAULT_USERNAME,
//    IResourceBuilder<RabbitMQServerResource> messaging
//)
//{
//    x.EnvironmentVariables[EnvironmentVariablesConstants.MessageQueueHost] = messaging
//        .Resource
//        .PrimaryEndpoint
//        .Host;
//    x.EnvironmentVariables[EnvironmentVariablesConstants.MessageQueuePort] =
//        messaging.Resource.PrimaryEndpoint.Port.ToString();
//    x.EnvironmentVariables[EnvironmentVariablesConstants.MessageQueueUsername] =
//        MESSAGE_QUEUE_DEFAULT_USERNAME;
//    x.EnvironmentVariables[EnvironmentVariablesConstants.MessageQueuePassword] =
//        (await messaging.Resource.PasswordParameter.GetValueAsync(x.CancellationToken))
//        ?? throw new InvalidOperationException("Password for message queue not found");
//}

static IResourceBuilder<RabbitMQServerResource> AddMessaging(IDistributedApplicationBuilder builder)
{
    var messagingUsername = builder.AddParameter("messaging-username", "guest");
    var messagingPassword = builder.AddParameter("messaging-password", "guest", secret: true);
    return builder
        .AddRabbitMQ("messaging", messagingUsername, messagingPassword, 5672)
        .WithContainerName("wband-rabbitmq")
        .WithManagementPlugin(15672)
        .WithDataVolume("wband-rabbitmq")
        .WithLifetime(ContainerLifetime.Persistent);
}

static IResourceBuilder<PostgresServerResource> AddPostgres(IDistributedApplicationBuilder builder)
{
    var postgresUsername = builder.AddParameter("postgres-username", "postgres");
    var postgresPassword = builder.AddParameter("postgres-password", "postgres", secret: true);
    return builder
        .AddPostgres("postgres", postgresUsername, postgresPassword, 5432)
        .WithContainerName("wband-postgres")
        .WithDataVolume("wband-postgres")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithPgAdmin(
            x => x.WithLifetime(ContainerLifetime.Persistent).WithHostPort(5050),
            "wband-pgadmin"
        );
}

static async Task<IResourceBuilder<KeycloakResource>> AddKeycloak(
    IDistributedApplicationBuilder builder,
    IResourceBuilder<RabbitMQServerResource> messaging
)
{
    var keycloakUsername = builder.AddParameter("username", "admin");
    var keycloakPassword = builder.AddParameter("password", "admin", secret: true);
    return builder
        .AddKeycloak("keycloak", 8080, keycloakUsername, keycloakPassword)
        .WithDataVolume("wband-keycloak")
        .WithBindMount("keycloak/providers", "/opt/keycloak/providers")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithEnvironment("KC_HOSTNAME_URL", "http://localhost/identity-api")
        .WithEnvironment("KK_TO_RMQ_URL", messaging.Resource.Name)
        .WithEnvironment(
            "KK_TO_RMQ_PORT",
            messaging.Resource.PrimaryEndpoint.Property(EndpointProperty.Port)
        )
        .WithEnvironment(
            "KK_TO_RMQ_USERNAME",
            (await messaging.Resource.UserNameReference.GetValueAsync(default))
        )
        .WithEnvironment(
            "KK_TO_RMQ_PASSWORD",
            (await messaging.Resource.PasswordParameter!.GetValueAsync(default))
        )
        //.WithEnvironment("KK_TO_RMQ_EXCHANGE", "keycloak-events")
        .WithEnvironment("KK_TO_RMQ_VHOST", "/")
        .WithRealmImport("./keycloak/realms");
}

static IResourceBuilder<MinioContainerResource> AddMinio(IDistributedApplicationBuilder builder)
{
    //var minioUsername = builder.AddParameter("minio-username", "minioadmin");
    //var minioPassword = builder.AddParameter("minio-password", "minioadmin123", secret: true);
    return builder
        .AddMinioContainer("minio", port: 9000)
        .WithContainerName("wband-minio")
        .WithImageTag("RELEASE.2025-04-22T22-12-26Z")
        .WithDataVolume("wband-minio")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
        .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin123");
}
