using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var postgresUsername = builder.AddParameter("postgres-username", "postgres");
var postgresPassword = builder.AddParameter("posthres-password", "postgres", secret: true);
var postgresServer = builder
    .AddPostgres("postgres", postgresUsername, postgresPassword)
    // Keep pooled database connections off the development TCP proxy.
    .WithEndpoint(
        "tcp",
        endpoint =>
        {
            endpoint.Port = builder.Configuration.GetValue("Postgres:Port", 25432);
            endpoint.IsProxied = false;
        }
    )
    .WithDataVolume("wband-postgres")
    .WithPgAdmin(x => x.WithLifetime(ContainerLifetime.Persistent))
    .WithLifetime(ContainerLifetime.Persistent);
var postgresDatabase = postgresServer.AddDatabase("wband");

var rabbitmqUsername = builder.AddParameter("rabbitmq-username", "guest");
var rabbitmqPassword = builder.AddParameter("rabbitmq-password", "guest", secret: true);
var rabbitmqServer = builder
    .AddRabbitMQ("rabbitmq", rabbitmqUsername, rabbitmqPassword)
    // Keep long-lived AMQP connections off the development TCP proxy.
    // An unproxied container endpoint requires an explicit published port.
    .WithEndpoint(
        "tcp",
        endpoint =>
        {
            endpoint.Port = builder.Configuration.GetValue("RabbitMQ:Port", 25673);
            endpoint.IsProxied = false;
        }
    )
    .WithDataVolume("wband-rabbitmq")
    .WithManagementPlugin()
    // Development-only: the provider connects from another container using the default guest account.
    .WithEnvironment("RABBITMQ_SERVER_ADDITIONAL_ERL_ARGS", "-rabbit loopback_users []")
    .WithLifetime(ContainerLifetime.Persistent);

var cache = builder
    .AddRedis("redis")
    .WithDataVolume("wband-redis")
    .WithLifetime(ContainerLifetime.Persistent);

var rustfsServer = builder
    .AddRustFs("rustfs")
    .WithDataVolume("wband-rustfs")
    .WithLifetime(ContainerLifetime.Persistent);
var rustfs = rustfsServer.AddBucket("wband-bucket");

var keycloakUsername = builder.AddParameter("keycloak-username", "admin");
var keycloakPassword = builder.AddParameter("keycloak-password", "qweQWE123!@#", secret: true);
var bffClientSecret = builder.AddParameter(
    "bff-client-secret",
    "wband-local-development-only",
    secret: true
);
var webapiClientSecret = builder.AddParameter(
    "webapi-client-secret",
    "wband-webapi-local-development-only",
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
    .WithBindMount(
        "./keycloak/quarkus.properties",
        "/opt/keycloak/conf/quarkus.properties",
        isReadOnly: true
    )
    .WithRealmImport("./keycloak/realms")
    .WaitFor(rabbitmqServer)
    .WithEnvironment("BFF_CLIENT_SECRET", bffClientSecret)
    .WithEnvironment("WEBAPI_CLIENT_SECRET", webapiClientSecret)
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
    .WithEnvironment("KEYCLOAK_URI", keycloakEndpoint)
    .WithEnvironment("KEYCLOAK_REALM", "wband-dev")
    .WithEnvironment("KEYCLOAK_CLIENT_ID", "webapi")
    .WithEnvironment("KEYCLOAK_CLIENT_SECRET", webapiClientSecret)
    .WithEnvironment("KEYCLOAK_SSL_REQUIRED", "false")
    .WithEnvironment(
        "DATABASE_CONNECTION_STRING",
        postgresDatabase.Resource.ConnectionStringExpression
    )
    .WithEnvironment(
        "RABBITMQ_HOST",
        rabbitmqServer.Resource.PrimaryEndpoint.Property(EndpointProperty.Host)
    )
    .WithEnvironment(
        "RABBITMQ_PORT",
        rabbitmqServer.Resource.PrimaryEndpoint.Property(EndpointProperty.Port)
    )
    .WithEnvironment("RABBITMQ_USERNAME", rabbitmqUsername)
    .WithEnvironment("RABBITMQ_PASSWORD", rabbitmqPassword)
    .WithEnvironment("RABBITMQ_VIRTUAL_HOST", "/")
    .WithEnvironment("RABBITMQ_SSL_ENABLED", "false")
    .WithEnvironment("KEYCLOAK_EVENTS_EXCHANGE", "amq.topic")
    .WithEnvironment("KEYCLOAK_EVENTS_QUEUE", "wband-user-module-keycloak-events")
    .WithEnvironment("S3_HOST", rustfs.Resource.Parent.PrimaryEndpoint)
    .WithEnvironment("S3_BUCKET", "wband-bucket")
    .WithEnvironment("S3_ACCESS_KEY", rustfs.Resource.Parent.AccessKey)
    .WithEnvironment("S3_SECRET_KEY", rustfs.Resource.Parent.SecretKey);

// builder
//     .AddProject<Projects.WBand_MixerWorker>("mixer-worker")
//     .WithReference(postgresDatabase)
//     .WaitFor(postgresDatabase)
//     .WithReference(rustfs)
//     .WaitFor(rustfs)
//     .WithEnvironment(
//         "MARTEN_DATABASE_CONNECTION_STRING",
//         postgresDatabase.Resource.ConnectionStringExpression
//     )
//     .WithEnvironment("MARTEN_DATABASE_SCHEMA_NAME", "marten")
//     .WithEnvironment("S3_UPLOAD_EXPIRATION_SECONDS", "900")
//     .WithEnvironment("S3_DOWNLOAD_EXPIRATION_SECONDS", "604800")
//     .WithEnvironment("S3_HOST", rustfs.Resource.Parent.PrimaryEndpoint)
//     .WithEnvironment("S3_BUCKET", "wband-bucket")
//     .WithEnvironment("S3_ACCESS_KEY", rustfs.Resource.Parent.AccessKey)
//     .WithEnvironment("S3_SECRET_KEY", rustfs.Resource.Parent.SecretKey);

#pragma warning disable ASPIREJAVASCRIPT001
var web = builder.AddNextJsApp("web", "../web", "dev").WithBun().WithExternalHttpEndpoints();

#pragma warning restore

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

// Server-rendered Next.js routes validate the same BFF session as browser requests.
// An endpoint reference supplies the address without making web wait for proxy:
// proxy already waits for web, so adding a lifecycle dependency would create a cycle.
web.WithEnvironment("BFF_ORIGIN", proxy.GetEndpoint("http"));
// Regenerate only after HTTP contract changes, never as a side effect of normal startup.
if (builder.Configuration.GetValue<bool>("GenerateApiClients"))
{
    builder
        .AddExecutable("generate-api-clients", "bun", "../web", "run", "generate-api-clients")
        .WithEnvironment(
            "WEBAPI_OPENAPI",
            ReferenceExpression.Create($"{webapi.GetEndpoint("http")}/openapi/v1.json")
        )
        .WithEnvironment(
            "AUTH_OPENAPI",
            ReferenceExpression.Create($"{proxy.GetEndpoint("http")}/openapi/v1.json")
        )
        .WaitFor(webapi)
        .WaitFor(proxy);
}

// Signed uploads and downloads go from the browser directly to the S3 endpoint.
// Use browser origins: Aspire rewrites endpoint hosts inside containers to aspire.dev.internal.
rustfsServer.WithEnvironment(
    "RUSTFS_CORS_ALLOWED_ORIGINS",
    ReferenceExpression.Create(
        $"http://localhost:{proxy.GetEndpoint("http").Property(EndpointProperty.Port)},http://localhost:{web.GetEndpoint("http").Property(EndpointProperty.Port)}"
    )
);

builder.Build().Run();
