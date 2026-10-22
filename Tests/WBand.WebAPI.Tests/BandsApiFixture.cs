using WBand.Modules.BandModule.Endpoints;
using BandDataContext = WBand.Modules.BandModule.Data.BandDataContext;
using WBand.Modules.BandModule;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using WBand.WebAPI.Data;
using WBand.WebAPI.Endpoints.Bands;
using WBand.WebAPI.Services;
using WBand.WebAPI.Services.Implementations;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Xunit;
using WBand.Modules.EventModule;

namespace WBand.WebAPI.Tests;

[CollectionDefinition("Web API")]
public sealed class WebApiCollection : ICollectionFixture<BandsApiFixture>
{
}

public sealed class BandsApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:18.3")
        .WithCommand("-c", "log_connections=on", "-c", "log_disconnections=on")
        .Build();
    private WebApplication app = null!;
    public bool RejectPublications { get; set; }

    public string ConnectionString => postgres.GetConnectionString();
    public IServiceProvider Services => app.Services;

    public DataContext CreateContext() => new(new DbContextOptionsBuilder<DataContext>().UseNpgsql(ConnectionString).Options);

    public BandDataContext CreateBandContext() => new(Services.GetRequiredService<DbContextOptions<BandDataContext>>());

    public HttpClient CreateClient(Guid? userId = null, string email = "owner@example.com")
    {
        var client = app.GetTestClient();
        if (userId is not null)
        {
            // Authenticated clients represent registered users in the local database.
            using var db = CreateBandContext();
            db.Database.ExecuteSqlRaw("""
                insert into bands.users (id, keycloak_id, email, first_name, last_name, created_at, updated_at, last_login_at)
                values (@id, @subject, @email, '', '', clock_timestamp(), clock_timestamp(), @lastLogin)
                on conflict (keycloak_id) do nothing
                """, new NpgsqlParameter("id", Guid.CreateVersion7()), new NpgsqlParameter("subject", userId.Value), new NpgsqlParameter("email", $"{userId.Value:N}@example.com"), new NpgsqlParameter("lastLogin", default(DateTimeOffset)));
            client.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
            client.DefaultRequestHeaders.Add("X-Test-Email", email);
        }
        return client;
    }

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();
        await DatabaseSetup.ApplyMigrations(ConnectionString, default);
        await DatabaseSetup.ApplyMigrations(ConnectionString, default);
        await using (var db = CreateContext())
            await db.ExecuteAsync("ALTER TABLE bands.bands ADD CONSTRAINT test_commit_failure CHECK (name <> 'fail_commit')");

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, BandsTestAuthenticationHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();
        builder.Services.AddValidatorsFromAssembly(typeof(WBand.WebAPI.Data.DataContext).Assembly, ServiceLifetime.Singleton);
        builder.Services.AddWolverineHttp();
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
        builder.Services.AddDatabase(ConnectionString);
        builder.Services.AddBandModule(ConnectionString);
        builder.Services.AddEventModule(ConnectionString);
        builder.UseWolverine(options =>
        {
            options.MetadataRules.Add(new RejectingOutgoingRule(() => RejectPublications));
            // Keep the failure path observable even without test event subscribers.
            options.PublishAllMessages().ToLocalQueue("test-outgoing");
            options.Discovery.IncludeAssembly(typeof(WBand.WebAPI.Data.DataContext).Assembly);
            BandModule.Configure(options);
            options.Discovery.IncludeAssembly(typeof(EventModule).Assembly);
            options.UseFluentValidation();
        });
        app = builder.Build();
        await app.Services.MigrateEvents();
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapOpenApi().AllowAnonymous();
        app.MapWolverineEndpoints(options => options.UseFluentValidationProblemDetailMiddleware());
        await app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (app is not null)
            await app.DisposeAsync();
        try
        {
            await PostgresDiagnostics.SaveLogs(postgres);
        }
        finally
        {
            await postgres.DisposeAsync();
        }
    }
}

public sealed class BandsTestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Guid.TryParse(Request.Headers["X-Test-User"], out var userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(
            [new Claim("sub", userId.ToString()), new Claim("email", Request.Headers["X-Test-Email"].ToString())],
            Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
