using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WBand.WebAPI.Data;
using WBand.WebAPI.Endpoints;
using WBand.WebAPI.Services.Implementations;
using Xunit;
using Moq;
using Wolverine;
using WBand.WebAPI.Application;
using WBand.WebAPI.Events;
using WBand.Modules.UserModule.Events;

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
        var response = await CurrentUserEndpoint.Get(currentUser, db, Mock.Of<IMessageBus>(), default);
        Assert.NotEqual(user.Id, user.KeycloakId);
        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal("Anna Ivanova", response.DisplayName);
    }
    [Fact]
    public async Task Get_WhenRegistrationWasMissed_CreatesProfileOnceAcrossConcurrentRequests()
    {
        var subject = Guid.NewGuid();
        var identity = new CurrentUser(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim("sub", subject.ToString()),
                    new Claim("email", $"{subject:N}@example.com"),
                    new Claim("given_name", "Anna"), new Claim("family_name", "Ivanova"),
                ], "Bearer")),
            },
        });
        var bus = new Mock<IMessageBus>();
        async Task<CurrentUserEndpoint.CurrentUserResponse> Get()
        {
            await using var db = fixture.CreateSession();
            return await CurrentUserEndpoint.Get(identity, db, bus.Object, default);
        }
        var profiles = await Task.WhenAll(Get(), Get(), Get());
        Assert.All(profiles, profile =>
        {
            Assert.Equal(profiles[0].Id, profile.Id);
            Assert.NotEqual(subject, profile.Id);
            Assert.Equal("Anna Ivanova", profile.DisplayName);
        });
        // A delayed registration must preserve the restored profile and its local identifier.
        await using var session = fixture.CreateSession();
        await KeycloakEventHandler.Handle(new KeycloakEvent
        {
            Type = "REGISTER", UserId = subject, Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Details = new Dictionary<string, string> { ["email"] = identity.Email },
        }, session, bus.Object, default);
        Assert.Equal(profiles[0].Id, (await Get()).Id);
        bus.Verify(x => x.PublishAsync(It.IsAny<UserCreated>(), It.IsAny<DeliveryOptions>()), Times.Once);
    }

}
