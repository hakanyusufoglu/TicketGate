namespace TicketGate.API.Configuration;

/// <summary>
/// ASP.NET Core rate limiting policy ayarlarini tasir.
/// Limit ve pencere degerleri appsettings uzerinden okunur; kod icinde magic number tutulmaz.
/// </summary>
public sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; init; }

    public RateLimitPolicySettings Auth { get; init; } = new();

    public RateLimitPolicySettings Reserve { get; init; } = new();

    public RateLimitPolicySettings Queue { get; init; } = new();

    public RateLimitPolicySettings Read { get; init; } = new();

    public RateLimitPolicySettings Sse { get; init; } = new();
}

/// <summary>
/// Tek bir fixed-window rate limiting policy'sinin sayisal ayarlarini tasir.
/// Permit, pencere suresi ve kuyruk limiti konfigurasyondan gelir.
/// </summary>
public sealed class RateLimitPolicySettings
{
    public int PermitLimit { get; init; }

    public int WindowSeconds { get; init; }

    public int QueueLimit { get; init; }
}
