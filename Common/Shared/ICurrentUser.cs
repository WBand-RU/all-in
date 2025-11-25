namespace Shared;

public interface ICurrentUser
{
    string GetUserId { get; }
    string Role { get; }
    string GetUserEmail { get; }
}
