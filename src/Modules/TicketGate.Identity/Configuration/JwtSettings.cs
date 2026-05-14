namespace TicketGate.Identity.Configuration;

/// <summary>
/// JWT token uretimi ve validasyonu icin yapilandirma ayarlari.
/// Token sureleri ve clock skew kod icinde magic number olarak tutulmaz.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; init; } = 15;

    public int RefreshTokenExpirationDays { get; init; } = 7;

    public int ClockSkewMinutes { get; init; } = 2;
}
