using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TicketGate.Core.Extensions;
using TicketGate.Identity.Features.Auth.Commands.LoginUser;
using TicketGate.Identity.Features.Auth.Commands.RefreshToken;
using TicketGate.Identity.Features.Auth.Commands.RegisterUser;

namespace TicketGate.Identity.Features.Auth.Endpoints;

public static class IdentityEndpoints
{
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
        });

        group.MapPost("/login", async (
            LoginUserCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/refresh", async (
            RefreshTokenCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        });

        return app;
    }
}
