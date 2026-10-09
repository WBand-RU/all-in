using AuthorizedProxy;
using WBand.WebProxy;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);
var configuration =
    builder.Configuration.Get<Configuration>()
    ?? throw new InvalidOperationException("Proxy configuration is required.");

builder.Configuration["AUTHORIZED_PROXY_OAUTH_VALID_ISSUERS"] = builder.Configuration[
    "AUTHORIZED_PROXY_OAUTH_AUTHORITY"
];
builder.AddAutorizedProxy(GetRoutes, GetClusters);
builder.AddWBandAuthentication();
builder.Services.AddSingleton<
    Yarp.ReverseProxy.Transforms.Builder.ITransformProvider,
    SessionTokenTransform
>();

var app = builder.Build();
app.UseWBandAuthentication();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.MapReverseProxy();
await app.RunAsync();

IReadOnlyList<RouteConfig> GetRoutes() =>
    [
        new()
        {
            RouteId = "web",
            ClusterId = "web",
            Order = 100,
            AuthorizationPolicy = "anonymous",
            Match = new() { Path = "{**catch-all}" },
        },
        .. new[]
        {
            "api",
            "band-api",
            "song-api",
            "playlist-api",
            "stem-api",
            "mixer-api",
            "playback-api",
            "files",
        }.Select(prefix => new RouteConfig
        {
            RouteId = prefix,
            ClusterId = "api",
            Order = 0,
            AuthorizationPolicy = "default",
            Match = new() { Path = $"/{prefix}/{{**catch-all}}" },
        }),
    ];

IReadOnlyList<ClusterConfig> GetClusters() =>
    [
        CreateCluster(configuration.WebCommaSplitAddresses, "web"),
        CreateCluster(configuration.ApiCommaSplitAddresses, "api"),
    ];

static ClusterConfig CreateCluster(string addresses, string id)
{
    var destinations = addresses
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Select((address, index) => new { address, index })
        .ToDictionary(
            x => $"destination-{x.index}",
            x => new DestinationConfig { Address = x.address }
        );
    if (
        destinations.Count == 0
        || destinations.Values.Any(x =>
            !Uri.TryCreate(x.Address, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
        )
    )
        throw new InvalidOperationException(
            $"Cluster '{id}' requires HTTP(S) destination addresses."
        );
    return new ClusterConfig { ClusterId = id, Destinations = destinations };
}

/// <summary>Exposes the proxy entry point to integration tests.</summary>
public partial class Program;
