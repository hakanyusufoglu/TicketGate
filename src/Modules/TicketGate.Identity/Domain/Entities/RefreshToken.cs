namespace TicketGate.Identity.Domain.Entities;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    private RefreshToken(Guid id, string token, Guid userId, DateTime expiresAt, DateTime createdAt)
    {
        Id = id;
        Token = token;
        UserId = userId;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Token { get; private set; } = string.Empty;

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public DateTime ExpiresAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsActive => !IsExpired && !IsRevoked;

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt)
    {
        return new RefreshToken(Guid.NewGuid(), token, userId, expiresAt, DateTime.UtcNow);
    }

    public void Revoke()
    {
        RevokedAt = DateTime.UtcNow;
    }
}
