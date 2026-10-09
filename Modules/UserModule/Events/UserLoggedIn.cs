namespace WBand.Modules.UserModule.Events;

public record UserLoggedIn(Guid UserId, string UserName, DateTime LoginTime);
