using WBand.Modules.UserModule.Events;
using WBand.WebAPI.Application;
using WBand.WebAPI.Data;
using WBand.WebAPI.Events;
using Xunit;
using Moq;
using Wolverine;

namespace WBand.WebAPI.Tests;

[Collection("Database")]
public sealed class KeycloakEventHandlerTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Handle_WhenRegistered_CommitsUserThenPublishesUserCreated()
    {
        var message = CreateRegistration();
        await Handle(message);
        await using var db = fixture.CreateSession();
        var user = await FindUser(message.UserId, db);
        Assert.NotNull(user);
        Assert.Equal(message.Details["email"].Trim().ToLowerInvariant(), user.Email);
        Assert.Equal(default, user.LastLoginAt);
        var rows = await EventsFor(user.Id, db);
        var row = Assert.Single(rows);
        Assert.Equal(user.Id, Assert.IsType<UserCreated>(row).UserId);
    }

    [Fact]
    public async Task Handle_WhenLoginIsRedelivered_UpdatesOnceWithoutRequiringEmail()
    {
        var message = CreateRegistration();
        await Handle(message);
        var login = message with { Type = "LOGIN", Time = message.Time + 1000, Details = new Dictionary<string, string>() };
        await Handle(login);
        await Handle(login);
        await using var db = fixture.CreateSession();
        var user = (await FindUser(message.UserId, db))!;
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(login.Time), user.LastLoginAt);
        Assert.Equal(2, (await EventsFor(user.Id, db)).Count);
    }

    [Fact]
    public async Task Handle_WhenOlderLoginArrives_DoesNotOverwriteNewerTimestamp()
    {
        var registration = CreateRegistration();
        await Handle(registration);
        var newer = registration with { Type = "LOGIN", Time = registration.Time + 2000 };
        await Handle(newer);
        await Handle(newer with { Time = registration.Time + 1000 });
        await using var db = fixture.CreateSession();
        var user = (await FindUser(registration.UserId, db))!;
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(newer.Time), user.LastLoginAt);
        Assert.Equal(2, (await EventsFor(user.Id, db)).Count);
    }

    [Fact]
    public async Task Handle_WhenEmailIsMissing_RollsBackWithoutCreatingUser()
    {
        var message = CreateRegistration() with { Details = new Dictionary<string, string>() };
        await Assert.ThrowsAsync<InvalidOperationException>(() => Handle(message));
        await using var db = fixture.CreateSession();
        Assert.Null(await FindUser(message.UserId, db));
    }

    [Fact]
    public async Task Handle_WhenLoginHasNoRegisteredUser_ThrowsWithoutCreatingUser()
    {
        var message = CreateRegistration() with { Type = "LOGIN", Details = new Dictionary<string, string>() };
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Handle(message));
        Assert.Contains("registered", error.Message);
        await using var db = fixture.CreateSession();
        Assert.Null(await FindUser(message.UserId, db));
    }

    [Fact]
    public async Task Handle_WhenRegistrationIsRedelivered_CreatesOneUserAndEvent()
    {
        var message = CreateRegistration();
        await Task.WhenAll(Handle(message), Handle(message));
        await using var db = fixture.CreateSession();
        var user = (await FindUser(message.UserId, db))!;
        Assert.Single(await EventsFor(user.Id, db));
    }

    [Fact]
    public async Task Handle_WhenPublicationFails_UserRemainsCommitted()
    {
        var message = CreateRegistration();
        var bus = new Mock<IMessageBus>();
        bus.Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions>()))
            .Returns(ValueTask.FromException(new InvalidOperationException("broker unavailable")));
        await using var db = fixture.CreateSession();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            KeycloakEventHandler.Handle(message, db, bus.Object, default));
        await using var reader = fixture.CreateSession();
        Assert.NotNull(await FindUser(message.UserId, reader));
    }

    private async Task Handle(KeycloakEvent message)
    {
        await using var db = fixture.CreateSession();
        await KeycloakEventHandler.Handle(message, db, fixture.Publisher.Bus.Object, default);
    }

    private static Task<User?> FindUser(Guid id, DataContext db) =>
        db.SingleOrDefaultAsync<User>("select * from public.users where keycloak_id = @id", new { id });

    private Task<IReadOnlyList<object>> EventsFor(Guid id, DataContext db) =>
        Task.FromResult<IReadOnlyList<object>>(fixture.Publisher.Messages.Values.Where(message => message switch
        {
            UserCreated created => created.UserId == id,
            UserLoggedIn login => login.UserId == id,
            _ => false,
        }).ToArray());

    private static KeycloakEvent CreateRegistration()
    {
        var id = Guid.NewGuid();
        return new KeycloakEvent
        {
            Type = "REGISTER", UserId = id, Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Details = new Dictionary<string, string> { ["email"] = $" {id:N}@Example.com " },
        };
    }

}
