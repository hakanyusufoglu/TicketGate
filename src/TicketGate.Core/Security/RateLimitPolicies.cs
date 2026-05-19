namespace TicketGate.Core.Security;

/// <summary>
/// Endpoint gruplarinin kullandigi rate limiting policy adlarini merkezi tutar.
/// Moduller API projesine referans vermeden ayni policy sozlesmesini kullanir.
/// </summary>
public static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string Reserve = "reserve";
    public const string Queue = "queue";
    public const string Read = "read";
    public const string Sse = "sse";
}
