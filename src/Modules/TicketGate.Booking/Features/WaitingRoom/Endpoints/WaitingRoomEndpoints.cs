using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TicketGate.Booking.Features.WaitingRoom.Commands.JoinQueue;
using TicketGate.Booking.Features.WaitingRoom.Commands.LeaveQueue;
using TicketGate.Booking.Features.WaitingRoom.Queries.GetQueuePosition;
using TicketGate.Core.Extensions;
using TicketGate.Core.Security;

namespace TicketGate.Booking.Features.WaitingRoom.Endpoints;

/// <summary>
/// Waiting room HTTP endpoint'leri.
/// Tum is mantigi handler'larda; endpoint sadece HTTP donusumu yapar.
/// </summary>
public static class WaitingRoomEndpoints
{
    /// <summary>
    /// Waiting room endpoint'lerini /api/v1/queue grubu altinda kaydeder.
    /// Minimal API katmani path ve body degerlerini command/query nesnelerine cevirir.
    /// </summary>
    public static IEndpointRouteBuilder MapWaitingRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/queue").WithTags("WaitingRoom");

        group.MapPost("/{eventId:guid}/join", async (
            Guid eventId,
            [FromServices] IMediator mediator,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var userId = context.GetUserId();
            var result = await mediator.Send(new JoinQueueCommand(eventId, userId), cancellationToken);
            return result.ToHttpResult(StatusCodes.Status201Created);
        })
            .WithName("JoinQueue")
            .WithSummary("Waiting room kuyruguna katilir")
            .WithDescription("""
                Kullanici virtual waiting room kuyruguna eklenir.
                Kapasite bossa direkt checkout hakki verilebilir; kapasite doluysa Redis Sorted Set'e ZADD NX ile eklenir.
                UserId JWT token'dan okunur; body'den alinmaz.
                """)
            .Produces<JoinQueueResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Queue);

        group.MapGet("/{eventId:guid}/position", async (
            Guid eventId,
            [FromServices] IMediator mediator,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var userId = context.GetUserId();
            var result = await mediator.Send(new GetQueuePositionQuery(eventId, userId), cancellationToken);
            return result.ToHttpResult();
        })
            .WithName("GetQueuePosition")
            .WithSummary("Waiting room pozisyonunu getirir")
            .WithDescription("""
                Kullanici waiting room pozisyonunu Redis ZRANK ile okur.
                UserId JWT token'dan okunur; query string'den alinmaz.
                """)
            .Produces<QueuePositionDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Read);

        group.MapDelete("/{eventId:guid}/leave", async (
            Guid eventId,
            [FromServices] IMediator mediator,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var userId = context.GetUserId();
            var result = await mediator.Send(new LeaveQueueCommand(eventId, userId), cancellationToken);
            return result.ToHttpResult(StatusCodes.Status204NoContent);
        })
            .WithName("LeaveQueue")
            .WithSummary("Waiting room'dan cikar")
            .WithDescription("""
                Kullanici virtual waiting room kuyrugundan cikarilir.
                Redis Sorted Set uzerinden ZREM ile silinir.
                UserId JWT token'dan okunur; body'den alinmaz.
                """)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Reserve);

        return app;
    }
}
