using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TicketGate.Core.Extensions;
using TicketGate.Core.Security;
using TicketGate.Identity.Features.Auth.Commands.LoginUser;
using TicketGate.Identity.Features.Auth.Commands.RefreshToken;
using TicketGate.Identity.Features.Auth.Commands.RegisterUser;

namespace TicketGate.Identity.Features.Auth.Endpoints;

/// <summary>
/// Kimlik dogrulama HTTP endpoint'lerini kaydeder.
/// Register, login ve refresh token akislarinda is kurallari handler katmaninda kalir.
/// </summary>
public static class IdentityEndpoints
{
    /// <summary>
    /// Auth endpoint'lerini /api/v1/auth grubu altinda yayinlar.
    /// Endpoint katmani yalnizca command'i MediatR'a iletir ve Result'i HTTP sonucuna cevirir.
    /// </summary>
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/register", async (
            RegisterUserCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(StatusCodes.Status201Created);
        })
            .WithName("RegisterUser")
            .WithSummary("Kullanici kaydi olusturur")
            .WithDescription("""
                Yeni kullanici kaydi olusturur.
                Email benzersizligi ve parola kurallari command validator ve handler tarafindan dogrulanir.
                """)
            .Produces<RegisterUserResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .RequireRateLimiting(RateLimitPolicies.Auth);

        group.MapPost("/login", async (
            LoginUserCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
            .WithName("LoginUser")
            .WithSummary("Kullanici girisi yapar")
            .WithDescription("""
                Email ve parola ile kullaniciyi dogrular.
                Basarili olursa access token, refresh token ve token bitis zamanini doner.
                """)
            .Produces<LoginUserResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .RequireRateLimiting(RateLimitPolicies.Auth);

        group.MapPost("/refresh", async (
            RefreshTokenCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
            .WithName("RefreshToken")
            .WithSummary("Token yeniler")
            .WithDescription("""
                Gecerli refresh token ile access token ve refresh token rotasyonu yapar.
                Gecersiz veya suresi dolmus token icin unauthorized sonucu doner.
                """)
            .Produces<LoginUserResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .RequireRateLimiting(RateLimitPolicies.Auth);

        return app;
    }
}
