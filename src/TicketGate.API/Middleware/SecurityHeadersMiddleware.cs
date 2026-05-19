namespace TicketGate.API.Middleware;

/// <summary>
/// Güvenlik header'larını her response'a ekler.
/// XSS, clickjacking ve content sniffing saldırılarını engeller.
/// Production'da zorunlu; development'ta da aktif.
/// </summary>
public sealed class SecurityHeadersMiddleware(
    RequestDelegate next)
{
    private const string ContentTypeOptionsHeader = "X-Content-Type-Options";
    private const string ContentTypeOptionsValue = "nosniff";
    private const string FrameOptionsHeader = "X-Frame-Options";
    private const string FrameOptionsValue = "DENY";
    private const string XssProtectionHeader = "X-XSS-Protection";
    private const string XssProtectionValue = "1; mode=block";
    private const string ReferrerPolicyHeader = "Referrer-Policy";
    private const string ReferrerPolicyValue = "strict-origin-when-cross-origin";
    private const string PermissionsPolicyHeader = "Permissions-Policy";
    private const string PermissionsPolicyValue = "camera=(), microphone=(), geolocation=()";

    /// <summary>
    /// Her response'a güvenlik header'larını ekler.
    /// SSE endpoint'leri için stream davranışını bozmadan genel hardening header'ları uygulanır.
    /// </summary>
    public async Task InvokeAsync(HttpContext ctx)
    {
        ctx.Response.Headers.Append(ContentTypeOptionsHeader, ContentTypeOptionsValue);
        ctx.Response.Headers.Append(FrameOptionsHeader, FrameOptionsValue);
        ctx.Response.Headers.Append(XssProtectionHeader, XssProtectionValue);
        ctx.Response.Headers.Append(ReferrerPolicyHeader, ReferrerPolicyValue);
        ctx.Response.Headers.Append(PermissionsPolicyHeader, PermissionsPolicyValue);

        await next(ctx);
    }
}
