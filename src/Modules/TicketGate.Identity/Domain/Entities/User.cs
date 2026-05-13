namespace TicketGate.Identity.Domain.Entities;

public sealed class User
{
    private User()
    {
    }

    private User(Guid id, string email, string passwordHash, string fullName, DateTime createdAt)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public static User Create(string email, string passwordHash, string fullName)
    {
        return new User(
            Guid.NewGuid(),
            email.Trim().ToLowerInvariant(),
            passwordHash,
            fullName.Trim(),
            DateTime.UtcNow);
    }
}
