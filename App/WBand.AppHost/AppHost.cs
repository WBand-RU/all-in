var builder = DistributedApplication.CreateBuilder(args);

var postgresUsername = builder.AddParameter("postgres-username", "postgres");
var postgresPassword = builder.AddParameter("posthres-password", "postgres", secret: true);
var postgresServer = builder
    .AddPostgres("postgres", postgresUsername, postgresPassword)
    .WithDataVolume("wband-postgres")
    .WithPgAdmin(x => x.WithLifetime(ContainerLifetime.Persistent))
    .WithLifetime(ContainerLifetime.Persistent);
var postgresDatabase = postgresServer.AddDatabase("wband");

var rabbitmqUsername = builder.AddParameter("rabbitmq-username", "guest");
var rabbitmqPassword = builder.AddParameter("rabbitmq-password", "guest", secret: true);
var rabbitmqServer = builder
    .AddRabbitMQ("rabbitmq", rabbitmqUsername, rabbitmqPassword)
    .WithDataVolume("wband-rabbitmq")
    .WithManagementPlugin()
    // Development-only: the provider connects from another container using the default guest account.
    .WithEnvironment("RABBITMQ_SERVER_ADDITIONAL_ERL_ARGS", "-rabbit loopback_users []")
    .WithLifetime(ContainerLifetime.Persistent);

var cache = builder
    .AddRedis("redis")
    .WithDataVolume("wband-redis")
    .WithLifetime(ContainerLifetime.Persistent);

var rustfs = builder
    .AddRustFs("rustfs")
    .WithDataVolume("wband-rustfs")
    .WithLifetime(ContainerLifetime.Persistent)
    .AddBucket("wband-bucket");

var keycloakUsername = builder.AddParameter("keycloak-username", "admin");
var keycloakPassword = builder.AddParameter("keycloak-password", "qweQWE123!@#", secret: true);
var bffClientSecret = builder.AddParameter(
    "bff-client-secret",
    "wband-local-development-only",
    secret: true
);
var keycloak = builder
    .AddKeycloak(
        "keycloak",
        port: 28080,
        adminUsername: keycloakUsername,
        adminPassword: keycloakPassword
    )
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithBindMount("./keycloak/providers", "/opt/keycloak/providers", isReadOnly: true)
    .WithRealmImport("./keycloak/realms")
    .WaitFor(rabbitmqServer)
    .WithEnvironment("WBAND_BFF_CLIENT_SECRET", bffClientSecret)
    .WithEnvironment(x =>
    {
        x.EnvironmentVariables.Add(
            "KK_TO_RMQ_URL",
            rabbitmqServer.Resource.PrimaryEndpoint.Property(EndpointProperty.Host)
        );
        x.EnvironmentVariables.Add(
            "KK_TO_RMQ_PORT",
            rabbitmqServer.Resource.PrimaryEndpoint.Property(EndpointProperty.Port)
        );
        x.EnvironmentVariables.Add("KK_TO_RMQ_USERNAME", rabbitmqUsername);
        x.EnvironmentVariables.Add("KK_TO_RMQ_PASSWORD", rabbitmqPassword);
        x.EnvironmentVariables.Add("KK_TO_RMQ_VHOST", "/");
        x.EnvironmentVariables.Add("KK_TO_RMQ_EXCHANGE", "amq.topic");
    });

var keycloakEndpoint = keycloak.GetEndpoint("http");
var authority = ReferenceExpression.Create($"{keycloakEndpoint}/realms/wband-dev");

var webapi = builder
    .AddProject<Projects.WBand_WebAPI>("webapi")
    .WithReference(postgresDatabase)
    .WaitFor(postgresDatabase)
    .WithReference(rabbitmqServer)
    .WaitFor(rabbitmqServer)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(rustfs)
    .WaitFor(rustfs)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithEnvironment("KEYCLOAK_URL", keycloakEndpoint)
    .WithEnvironment("KEYCLOAK_REALM", "wband-dev")
    .WithEnvironment("KEYCLOAK_CLIENT_ID", "webapi")
    .WithEnvironment("KEYCLOAK_SSL_REQUIRED", "false")
    .WithEnvironment(
        "MARTEN_DATABASE_CONNECTION_STRING",
        postgresDatabase.Resource.ConnectionStringExpression
    )
    .WithEnvironment(
        "MESSAGING_HOST",
        rabbitmqServer.Resource.PrimaryEndpoint.Property(EndpointProperty.Host)
    )
    .WithEnvironment(
        "MESSAGING_PORT",
        rabbitmqServer.Resource.PrimaryEndpoint.Property(EndpointProperty.Port)
    )
    .WithEnvironment("MESSAGING_USERNAME", rabbitmqUsername)
    .WithEnvironment("MESSAGING_PASSWORD", rabbitmqPassword)
    .WithEnvironment("MESSAGING_VIRTUAL_HOST", "/")
    .WithEnvironment(
        "WBAND_KEYCLOAK_EVENTS_HOST",
        rabbitmqServer.Resource.PrimaryEndpoint.Property(EndpointProperty.Host)
    )
    .WithEnvironment(
        "WBAND_KEYCLOAK_EVENTS_PORT",
        rabbitmqServer.Resource.PrimaryEndpoint.Property(EndpointProperty.Port)
    )
    .WithEnvironment("WBAND_KEYCLOAK_EVENTS_USERNAME", rabbitmqUsername)
    .WithEnvironment("WBAND_KEYCLOAK_EVENTS_PASSWORD", rabbitmqPassword)
    .WithEnvironment("WBAND_KEYCLOAK_EVENTS_VIRTUAL_HOST", "/")
    .WithEnvironment("WBAND_KEYCLOAK_EVENTS_SSL_ENABLED", "false")
    .WithEnvironment("WBAND_KEYCLOAK_EVENTS_REALM", "wband-dev")
    .WithEnvironment("WBAND_KEYCLOAK_EVENTS_EXCHANGE", "amq.topic")
    .WithEnvironment("S3_HOST", rustfs.Resource.Parent.PrimaryEndpoint)
    .WithEnvironment("S3_BUCKET", "wband-bucket")
    .WithEnvironment("S3_ACCESS_KEY", rustfs.Resource.Parent.AccessKey)
    .WithEnvironment("S3_SECRET_KEY", rustfs.Resource.Parent.SecretKey);

var web = builder.AddViteApp("web", "../web").WithBun().WithExternalHttpEndpoints();

var proxy = builder
    .AddProject<Projects.WBand_WebProxy>("proxy")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WaitFor(web)
    .WaitFor(webapi)
    .WithEnvironment("WEB_COMMA_SPLIT_ADDRESSES", web.GetEndpoint("http"))
    .WithEnvironment("API_COMMA_SPLIT_ADDRESSES", webapi.GetEndpoint("http"))
    .WithEnvironment(
        "AUTHORIZED_PROXY_REDIS_CONNECTION_STRING",
        cache.Resource.ConnectionStringExpression
    )
    .WithEnvironment("AUTHORIZED_PROXY_REDIS_KEY_PREFIX", "wband-proxy-dev:")
    .WithEnvironment("AUTHORIZED_PROXY_OAUTH_AUTHORITY", authority)
    .WithEnvironment("AUTHORIZED_PROXY_OAUTH_CLIENT_ID", "spa-bff-client")
    .WithEnvironment("AUTHORIZED_PROXY_OAUTH_CLIENT_SECRET", bffClientSecret);

builder.Build().Run();
