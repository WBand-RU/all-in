//using System.Text;
//using System.Text.Json;
//using BandService.Contracts;
//using CSharpFunctionalExtensions;
//using Microsoft.Extensions.Options;

//namespace BandService.Services;

//public sealed class KeycloakAdminClient(
//    IOptions<AppSettings> options,
//    ILogger<KeycloakAdminClient> logger
//)
//{
//    private readonly HttpClient httpClient = new();
//    private readonly string baseUrl = options.Value.KeycloakUrl;
//    private readonly string realm = options.Value.KeycloakRealm;
//    private readonly string clientId = options.Value.KeycloakClientId;
//    private readonly string clientSecret = options.Value.KeycloakClientSecret;
//    private string? accessToken;
//    private DateTime tokenExpiry = DateTime.MinValue;

//    private async Task<string> GetAdminTokenAsync(CancellationToken cancellationToken = default)
//    {
//        if (this.accessToken != null && DateTime.UtcNow < this.tokenExpiry)
//        {
//            return this.accessToken;
//        }

//        var tokenUrl = $"{this.baseUrl}/realms/{this.realm}/protocol/openid-connect/token";
//        var content = new FormUrlEncodedContent(
//            new Dictionary<string, string>
//            {
//                { "client_id", this.clientId },
//                { "client_secret", this.clientSecret },
//                { "grant_type", "client_credentials" },
//            }
//        );

//        var response = await this.httpClient.PostAsync(tokenUrl, content, cancellationToken);
//        _ = response.EnsureSuccessStatusCode();

//        var json = await response.Content.ReadAsStringAsync(cancellationToken);
//        using var doc = JsonDocument.Parse(json);
//        this.accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
//        var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
//        this.tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60);

//        return this.accessToken;
//    }

//    private async Task<HttpResponseMessage> RequestAsync(
//        string endpoint,
//        HttpMethod method,
//        object? body = null,
//        CancellationToken cancellationToken = default
//    )
//    {
//        var token = await this.GetAdminTokenAsync(cancellationToken);
//        var url = $"{this.baseUrl}/admin/realms/{this.realm}{endpoint}";

//        var request = new HttpRequestMessage(method, url);
//        request.Headers.Add("Authorization", $"Bearer {token}");

//        if (body != null)
//        {
//            var json = JsonSerializer.Serialize(body);
//            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
//        }

//        return await this.httpClient.SendAsync(request, cancellationToken);
//    }

//    public async Task<List<KeycloakUser>> GetUsersByRoleAsync(
//        string roleName,
//        CancellationToken cancellationToken = default
//    )
//    {
//        var response = await this.RequestAsync(
//            $"/roles/{roleName}/users",
//            HttpMethod.Get,
//            cancellationToken: cancellationToken
//        );
//        _ = response.EnsureSuccessStatusCode();

//        var json = await response.Content.ReadAsStringAsync(cancellationToken);
//        logger.LogInformation("Keyclock users: {Users}", json);
//        return JsonSerializer.Deserialize<List<KeycloakUser>>(json) ?? [];
//    }

//    public async Task<KeycloakUser> GetUserByIdAsync(
//        string userId,
//        CancellationToken cancellationToken = default
//    )
//    {
//        var response = await this.RequestAsync(
//            $"/users/{userId}",
//            HttpMethod.Get,
//            cancellationToken: cancellationToken
//        );
//        _ = response.EnsureSuccessStatusCode();

//        var json = await response.Content.ReadAsStringAsync(cancellationToken);
//        return JsonSerializer.Deserialize<KeycloakUser>(json)!;
//    }

//    public async Task<Result<string>> CreateUserAsync(
//        KeycloakUser user,
//        CancellationToken cancellationToken = default
//    )
//    {
//        var response = await this.RequestAsync("/users", HttpMethod.Post, user, cancellationToken);
//        if (!response.IsSuccessStatusCode)
//        {
//            var result = await response.Content.ReadAsStringAsync(cancellationToken);
//            return Result.Failure<string>("Error by request to Keycloak: " + result);
//        }

//        var location = response.Headers.Location?.ToString();
//        if (location is null)
//        {
//            return Result.Failure<string>("Location header not found");
//        }

//        return location.Split('/').Last();
//    }

//    public async Task UpdateUserAsync(
//        string userId,
//        KeycloakUser user,
//        CancellationToken cancellationToken = default
//    )
//    {
//        var response = await this.RequestAsync(
//            $"/users/{userId}",
//            HttpMethod.Put,
//            user,
//            cancellationToken
//        );
//        _ = response.EnsureSuccessStatusCode();
//    }

//    public async Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
//    {
//        var response = await this.RequestAsync(
//            $"/users/{userId}",
//            HttpMethod.Delete,
//            cancellationToken: cancellationToken
//        );
//        _ = response.EnsureSuccessStatusCode();
//    }

//    public async Task<List<KeycloakRole>> GetAvailableRolesAsync(
//        CancellationToken cancellationToken = default
//    )
//    {
//        var response = await this.RequestAsync(
//            "/roles",
//            HttpMethod.Get,
//            cancellationToken: cancellationToken
//        );
//        _ = response.EnsureSuccessStatusCode();

//        var json = await response.Content.ReadAsStringAsync(cancellationToken);
//        return JsonSerializer.Deserialize<List<KeycloakRole>>(json) ?? [];
//    }

//    public async Task AddRealmRolesToUserAsync(
//        string userId,
//        List<KeycloakRole> roles,
//        CancellationToken cancellationToken = default
//    )
//    {
//        var response = await this.RequestAsync(
//            $"/users/{userId}/role-mappings/realm",
//            HttpMethod.Post,
//            roles,
//            cancellationToken
//        );
//        _ = response.EnsureSuccessStatusCode();
//    }

//    public async Task ResetPasswordAsync(
//        string userId,
//        string password,
//        bool temporary,
//        CancellationToken cancellationToken = default
//    )
//    {
//        var credential = new
//        {
//            type = "password",
//            value = password,
//            temporary,
//        };

//        var response = await this.RequestAsync(
//            $"/users/{userId}/reset-password",
//            HttpMethod.Put,
//            credential,
//            cancellationToken
//        );
//        _ = response.EnsureSuccessStatusCode();
//    }
//}
