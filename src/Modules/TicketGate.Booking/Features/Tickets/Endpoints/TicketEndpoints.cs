using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TicketGate.Booking.Features.Tickets.Commands.CancelTicket;
using TicketGate.Booking.Features.Tickets.Commands.ConfirmTicket;
using TicketGate.Booking.Features.Tickets.Commands.ReserveTicket;
using TicketGate.Booking.Features.Tickets.Queries.GetAvailableSeats;
using TicketGate.Booking.Features.Tickets.Queries.GetTicketById;
using TicketGate.Core.Extensions;

namespace TicketGate.Booking.Features.Tickets.Endpoints;

/// <summary>
/// Bilet islemleri HTTP endpoint'leri.
/// Tum is mantigi handler'larda; endpoint sadece HTTP donusumu yapar.
/// </summary>
public static class TicketEndpoints
{
    /// <summary>
    /// Ticket endpoint'lerini /api/v1 grubu altinda kaydeder.
    /// Minimal API katmani yalnizca route verisini command ve query nesnelerine cevirir.
    /// </summary>
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Tickets");

        group.MapPost("/tickets/{id:guid}/reserve", async (
            Guid id,
            TicketUserRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ReserveTicketCommand(id, request.UserId), cancellationToken);
            return result.ToHttpResult(StatusCodes.Status201Created);
        });

        group.MapPost("/tickets/{id:guid}/confirm", async (
            Guid id,
            TicketUserRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ConfirmTicketCommand(id, request.UserId), cancellationToken);
            return result.ToHttpResult(StatusCodes.Status204NoContent);
        });

        group.MapPost("/tickets/{id:guid}/cancel", async (
            Guid id,
            TicketUserRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CancelTicketCommand(id, request.UserId), cancellationToken);
            return result.ToHttpResult(StatusCodes.Status204NoContent);
        });

        group.MapGet("/tickets/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetTicketByIdQuery(id), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapGet("/events/{id:guid}/seats", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAvailableSeatsQuery(id), cancellationToken);
            return result.ToHttpResult();
        });

        return app;
    }

    /// <summary>
    /// Kullanici id iceren basit HTTP request govdesidir.
    /// Gateway auth tamamlanana kadar command'lar kullanici bilgisini bu alan uzerinden alir.
    /// </summary>
    private sealed record TicketUserRequest(Guid UserId);
}
