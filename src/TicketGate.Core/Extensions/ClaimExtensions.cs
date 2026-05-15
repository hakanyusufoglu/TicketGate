using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace TicketGate.Core.Extensions;

/// <summary>
/// JWT claim'lerinden kullanici bilgisi okuma extension metodlari.
/// Tum modullerde tekrar eden claim okuma kodunu ortadan kaldirir.
/// </summary>
public static class ClaimExtensions
{
    private const string SubjectClaimType = "sub";

    /// <summary>
    /// JWT token'dan UserId'yi okur.
    /// Token gecersiz veya claim yoksa endpoint'in auth korumasi eksik oldugunu gosteren hata firlatir.
    /// </summary>
    public static Guid GetUserId(this HttpContext context)
    {
        var claim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst(SubjectClaimType)?.Value;

        if (claim is null || !Guid.TryParse(claim, out var userId))
        {
            throw new InvalidOperationException(
                "UserId claim not found. Endpoint must be protected with authorization.");
        }

        return userId;
    }

    /// <summary>
    /// JWT token'dan kullanici email bilgisini okur.
    /// Claim yoksa null donerek endpoint'in opsiyonel email akisini destekler.
    /// </summary>
    public static string? GetUserEmail(this HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.Email)?.Value
            ?? context.User.FindFirst("email")?.Value;
    }
}
