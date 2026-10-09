using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AuthorizedProxy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace WBand.WebProxy;

internal sealed class TokenRefreshService(
    IConnectionMultiplexer redis,
    IHttpClientFactory clients,
    IDataProtectionProvider protection,
    IOptions<AuthorizedProxyConfiguration> configuration
)
{
    private readonly IDataProtector protector = protection.CreateProtector("WBand.TokenRefresh.v1");

    public async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var expires = context.Properties.GetTokenValue("expires_at");
        var access = context.Properties.GetTokenValue("access_token");
        if (
            string.IsNullOrEmpty(access)
            || !DateTimeOffset.TryParse(
                expires,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var expiresAt
            )
        )
        {
            await RejectAsync(context);
            return;
        }
        if (expiresAt > DateTimeOffset.UtcNow.AddSeconds(30))
            return;
        var refresh = context.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refresh))
        {
            await RejectAsync(context);
            return;
        }

        var tokens = await RefreshAsync(refresh, context.HttpContext.RequestAborted);
        if (tokens is null)
        {
            await RejectAsync(context);
            return;
        }
        foreach (var token in tokens)
            context.Properties.UpdateTokenValue(token.Name, token.Value);
        SessionClaims.UpdateRoles(
            context.Principal!,
            context.Properties.GetTokenValue("access_token")!
        );
        context.ShouldRenew = true;
    }

    private async Task<AuthenticationToken[]?> RefreshAsync(
        string refresh,
        CancellationToken cancellationToken
    )
    {
        // A distributed lock and short-lived encrypted result prevent parallel requests/replicas
        // from using the same rotating refresh token twice.
        var database = redis.GetDatabase();
        var key =
            configuration.Value.RedisKeyPrefix
            + "refresh:"
            + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refresh)));
        var lockKey = key + ":lock";
        var owner = Guid.NewGuid().ToString("N");
        for (var attempt = 0; attempt < 100; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cached = await database.StringGetAsync(key);
            if (cached.HasValue)
                return JsonSerializer.Deserialize<AuthenticationToken[]>(
                    protector.Unprotect(cached.ToString())
                );
            if (
                !await database.StringSetAsync(
                    lockKey,
                    owner,
                    TimeSpan.FromSeconds(30),
                    When.NotExists
                )
            )
            {
                await Task.Delay(100, cancellationToken);
                continue;
            }
            try
            {
                cached = await database.StringGetAsync(key);
                if (cached.HasValue)
                    return JsonSerializer.Deserialize<AuthenticationToken[]>(
                        protector.Unprotect(cached.ToString())
                    );
                using var client = clients.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                using var response = await client.PostAsync(
                    configuration.Value.OAuthAuthority.TrimEnd('/')
                        + "/protocol/openid-connect/token",
                    new FormUrlEncodedContent(
                        new Dictionary<string, string>
                        {
                            ["grant_type"] = "refresh_token",
                            ["refresh_token"] = refresh,
                            ["client_id"] = configuration.Value.OAuthClientId,
                            ["client_secret"] = configuration.Value.OAuthClientSecret,
                        }
                    ),
                    cancellationToken
                );
                using var json = await JsonDocument.ParseAsync(
                    await response.Content.ReadAsStreamAsync(cancellationToken),
                    cancellationToken: cancellationToken
                );
                if (!response.IsSuccessStatusCode)
                {
                    if (
                        json.RootElement.TryGetProperty("error", out var error)
                        && error.GetString() == "invalid_grant"
                    )
                    {
                        await database.StringSetAsync(
                            key,
                            protector.Protect("null"),
                            TimeSpan.FromMinutes(2)
                        );
                        return null;
                    }
                    response.EnsureSuccessStatusCode();
                }
                var result = new List<AuthenticationToken>
                {
                    new()
                    {
                        Name = "access_token",
                        Value = json.RootElement.GetProperty("access_token").GetString()!,
                    },
                    new()
                    {
                        Name = "refresh_token",
                        Value = json.RootElement.TryGetProperty("refresh_token", out var rotated)
                            ? rotated.GetString()!
                            : refresh,
                    },
                    new()
                    {
                        Name = "expires_at",
                        Value = DateTimeOffset
                            .UtcNow.AddSeconds(
                                json.RootElement.GetProperty("expires_in").GetInt32()
                            )
                            .ToString("o", CultureInfo.InvariantCulture),
                    },
                };
                if (json.RootElement.TryGetProperty("id_token", out var idToken))
                    result.Add(new() { Name = "id_token", Value = idToken.GetString()! });
                await database.StringSetAsync(
                    key,
                    protector.Protect(JsonSerializer.Serialize(result)),
                    TimeSpan.FromMinutes(2)
                );
                return result.ToArray();
            }
            finally
            {
                await database.ScriptEvaluateAsync(
                    "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end",
                    [new RedisKey(lockKey)],
                    [new RedisValue(owner)]
                );
            }
        }
        throw new TimeoutException("Timed out waiting for the session token refresh.");
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(AuthenticationExtensions.CookieScheme);
    }
}
