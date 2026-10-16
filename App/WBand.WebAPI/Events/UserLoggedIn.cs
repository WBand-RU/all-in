namespace WBand.Modules.UserModule.Events;

public record UserLoggedIn(Guid UserId, string Email, DateTimeOffset LoginTime);
