namespace Shared.Services;

public interface ICurrentUser
{
    Guid GetUserId { get; }
    string GetUserEmail { get; }
    string? DisplayName { get; }
    bool IsInRole(string role);
}
