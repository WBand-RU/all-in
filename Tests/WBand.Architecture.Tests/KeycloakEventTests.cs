using Marten;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WBand.Modules.UserModule.Application;
using WBand.Modules.UserModule.Domain;
using WBand.Modules.UserModule.Infrastructure.Messaging;
using Wolverine.Marten;
using Xunit;

namespace WBand.Architecture.Tests;

public sealed class KeycloakEventTests
{
    [Theory]
    [InlineData("REGISTER")]
    [InlineData("LOGIN")]
    public async Task Handle_WhenProfileMissing_CreatesProfileWithKeycloakId(string type)
    {
        var session = new Mock<IDocumentSession>();
        UserProfile? saved = null;
        session.Setup(x => x.Insert(It.IsAny<UserProfile[]>())).Callback<UserProfile[]>(x => saved = x.Single());
        var message = Event(type);
        await Handle(message, session);
        Assert.NotNull(saved);
        Assert.Equal(message.UserId, saved.Id);
        Assert.Equal("new@example.com", saved.Email);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(message.Time), saved.CreatedAt);
        session.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("REGISTER", 2000)]
    [InlineData("LOGIN", 1000)]
    [InlineData("LOGIN", 500)]
    public async Task Handle_WhenEventDuplicateOrOlder_DoesNotOverwriteProfile(string type, long time)
    {
        var message = Event(type) with { Time = time };
        var session = new Mock<IDocumentSession>();
        var profile = new UserProfile { Id = message.UserId, Email = "current@example.com", LastLoginAt = DateTimeOffset.FromUnixTimeMilliseconds(1000) };
        session.Setup(x => x.LoadAsync<UserProfile>(message.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        await Handle(message, session);
        Assert.Equal("current@example.com", profile.Email);
        session.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLoginUsernameIsNotEmail_DoesNotInventEmail()
    {
        var session = new Mock<IDocumentSession>();
        UserProfile? saved = null;
        session.Setup(x => x.Insert(It.IsAny<UserProfile[]>())).Callback<UserProfile[]>(x => saved = x.Single());
        await Handle(Event("LOGIN") with { Details = new Dictionary<string, string> { ["username"] = "nickname" } }, session);
        Assert.Equal(string.Empty, saved!.Email);
        Assert.Equal("nickname", saved.DisplayName);
    }

    [Fact]
    public async Task Handle_WhenOtherEvent_DoesNotCreateProfile()
    {
        var session = new Mock<IDocumentSession>(MockBehavior.Strict);
        await Handle(Event("LOGOUT"), session);
        session.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenStorageFails_PropagatesFailureForRetry()
    {
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("database unavailable"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Handle(Event("REGISTER"), session));
    }

    private static KeycloakEvent Event(string type) => new()
    {
        Type = type, UserId = Guid.NewGuid(), Time = 2000,
        Details = new Dictionary<string, string> { ["email"] = " New@Example.COM ", ["username"] = "new" },
    };

    private static Task Handle(KeycloakEvent message, Mock<IDocumentSession> session) =>
        KeycloakEventHandler.Handle(message, session.Object, new Mock<IMartenOutbox>().Object,
            NullLogger<KeycloakEventHandler.KeycloakEventLog>.Instance, default);
}
