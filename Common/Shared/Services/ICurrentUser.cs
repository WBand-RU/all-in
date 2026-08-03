namespace Shared.Services;

public interface ICurrentUser
{
    Guid GetUserId { get; }
    string Role { get; }
    string GetUserEmail { get; }
}
