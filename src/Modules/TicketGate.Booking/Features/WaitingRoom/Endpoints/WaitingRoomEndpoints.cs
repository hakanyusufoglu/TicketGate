using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TicketGate.Booking.Features.WaitingRoom.Commands.JoinQueue;
using TicketGate.Booking.Features.WaitingRoom.Commands.LeaveQueue;
using TicketGate.Booking.Features.WaitingRoom.Queries.GetQueuePosition;
using TicketGate.Core.Extensions;

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
            [FromBody] QueueUserRequest request,
            [FromServices] ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new JoinQueueCommand(eventId, request.UserId), cancellationToken);
            return result.ToHttpResult(StatusCodes.Status201Created);
        });

        group.MapGet("/{eventId:guid}/position", async (
            Guid eventId,
            Guid userId,
            [FromServices] ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetQueuePositionQuery(eventId, userId), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapDelete("/{eventId:guid}/leave", async (
            Guid eventId,
            [FromBody] QueueUserRequest request,
            [FromServices] ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new LeaveQueueCommand(eventId, request.UserId), cancellationToken);
            return result.ToHttpResult(StatusCodes.Status204NoContent);
        });

        return app;
    }

    /// <summary>
    /// Waiting room endpoint'lerinde kullanici id tasiyan HTTP request govdesidir.
    /// Gateway auth tamamlanana kadar kullanici bilgisi bu alan uzerinden alinir.
    /// </summary>
    private sealed record QueueUserRequest(Guid UserId);
}
