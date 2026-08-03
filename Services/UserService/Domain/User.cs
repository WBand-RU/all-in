namespace UserService.Domain;

public class User
{
    public required UserId Id { get; init; }
    public Email Email { get; private set; }
    public DateTime RegisteredAt { get; } = DateTime.UtcNow;
    public DateTime? UpdateAt { get; private set; }

    public void SetName(Email newEmail)
    {
        this.Email = newEmail;
        this.UpdateAt = DateTime.UtcNow;
    }
}
