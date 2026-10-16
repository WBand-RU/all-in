namespace WBand.WebAPI.Services;

public interface ICurrentUser
{
    Guid Id { get; }
    string Email { get; }
    string FirstName { get; }
    string LastName { get; }
    bool IsInRole(string role);
}
