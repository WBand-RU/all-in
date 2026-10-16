using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WBand.WebAPI.Data;
using WBand.WebAPI.Endpoints;
using WBand.WebAPI.Services.Implementations;
using Xunit;

namespace WBand.WebAPI.Tests;

[Collection("Database")]
public sealed class CurrentUserEndpointTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Get_WhenSubjectMatchesKeycloakId_ReturnsStoredProfileUsingEfCore()
    {
        await using var db = fixture.CreateSession();
        var subject = Guid.NewGuid();
        var user = new User
        {
            KeycloakId = subject, Email = $"{subject:N}@example.com",
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
            LastLoginAt = DateTimeOffset.UtcNow, FirstName = "Anna", LastName = "Ivanova",
        };
        await using (var transaction = await db.BeginTransactionAsync())
        {
            await db.InsertAsync(user);
            await transaction.CommitAsync();
        }
        var currentUser = new CurrentUser(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject.ToString())], "Bearer")),
            },
        });
        var response = await CurrentUserEndpoint.Get(currentUser, db, default);
        Assert.NotEqual(user.Id, user.KeycloakId);
        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal("Anna Ivanova", response.DisplayName);
    }
}
