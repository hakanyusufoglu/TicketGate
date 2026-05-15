using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using TicketGate.Core.Extensions;

namespace TicketGate.Payment.Tests;

/// <summary>
/// JWT claim okuma extension metodlarini dogrular.
/// Endpoint katmani UserId bilgisini body/query yerine bu ortak extension uzerinden okumalidir.
/// </summary>
public sealed class ClaimExtensionsTests
{
    /// <summary>
    /// Standart JWT subject claim'i user id olarak okunabilmelidir.
    /// Login token'i sub claim'i urettigi icin Swagger akisi bu davranisa baglidir.
    /// </summary>
    [Fact]
    public void GetUserId_SubjectClaim_ReturnsUserId()
    {
        var userId = Guid.NewGuid();
        var context = CreateContext(new Claim("sub", userId.ToString()));

        var result = context.GetUserId();

        result.Should().Be(userId);
    }

    /// <summary>
    /// NameIdentifier claim'i user id olarak okunabilmelidir.
    /// Test ve framework map'li JWT akislarinda bu claim tipi kullanilir.
    /// </summary>
    [Fact]
    public void GetUserId_NameIdentifierClaim_ReturnsUserId()
    {
        var userId = Guid.NewGuid();
        var context = CreateContext(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        var result = context.GetUserId();

        result.Should().Be(userId);
    }

    /// <summary>
    /// UserId claim'i yoksa endpoint konfigürasyon hatasi net sekilde gorulmelidir.
    /// Bu extension yalnizca RequireAuthorization uygulanmis endpoint'lerde kullanilir.
    /// </summary>
    [Fact]
    public void GetUserId_MissingClaim_ThrowsInvalidOperationException()
    {
        var context = CreateContext(new Claim(ClaimTypes.Email, "user@ticketgate.test"));

        var act = () => context.GetUserId();

        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// Test icin claim principal iceren HTTP context olusturur.
    /// Endpoint katmanindaki HttpContext davranisini minimal sekilde taklit eder.
    /// </summary>
    private static HttpContext CreateContext(params Claim[] claims)
    {
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };
    }
}
