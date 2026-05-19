using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TicketGate.Booking.Features.Tickets.Commands.CancelTicket;
using TicketGate.Booking.Features.Tickets.Commands.GenerateTickets;
using TicketGate.Booking.Features.Tickets.Commands.ReserveTicket;
using TicketGate.Booking.Features.Tickets.Queries.GetAvailableSeats;
using TicketGate.Booking.Features.Tickets.Queries.GetTicketById;
using TicketGate.Core.Contracts;
using TicketGate.Core.Extensions;
using TicketGate.Core.Results;
using TicketGate.Core.Security;

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
            IMediator mediator,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var userId = context.GetUserId();
            var result = await mediator.Send(new ReserveTicketCommand(id, userId), cancellationToken);
            return result.ToHttpResult(StatusCodes.Status201Created);
        })
            .WithName("ReserveTicket")
            .WithSummary("Bilet rezerve eder")
            .WithDescription("""
                Belirtilen bileti rezerve eder.
                UserId JWT token'dan okunur; body'den alinmaz.
                Redis SETNX ile atomik kilit alinir ve TTL BookingSettings uzerinden uygulanir.
                Ayni bilete es zamanli istek gelirse 409 doner.
                """)
            .Produces<ReserveTicketResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Reserve);

        group.MapPost("/tickets/{id:guid}/cancel", async (
            Guid id,
            IMediator mediator,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var userId = context.GetUserId();
            var result = await mediator.Send(new CancelTicketCommand(id, userId), cancellationToken);
            return result.ToHttpResult(StatusCodes.Status204NoContent);
        })
            .WithName("CancelTicket")
            .WithSummary("Bileti iptal eder")
            .WithDescription("""
                Confirmed durumundaki bileti iptal eder.
                Confirmed durumundan Cancelled durumuna gecis yapilir.
                UserId JWT token'dan okunur; body'den alinmaz.
                """)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Reserve);

        group.MapGet("/tickets/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetTicketByIdQuery(id), cancellationToken);
            return result.ToHttpResult();
        })
            .WithName("GetTicketById")
            .WithSummary("Bilet detayini getirir")
            .WithDescription("""
                Id ile tekil bilet detayini getirir.
                Query handler projection-first okuma yapar.
                """)
            .Produces<TicketDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireRateLimiting(RateLimitPolicies.Read);

        group.MapGet("/events/{id:guid}/seats", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetAvailableSeatsQuery(id), cancellationToken);
            return result.ToHttpResult();
        })
            .WithName("GetAvailableSeats")
            .WithSummary("Musait koltuklari getirir")
            .WithDescription("""
                Event icin musait koltuk listesini getirir.
                Koltuk bilgileri section, row, seat number ve fiyat alanlariyla projection-first doner.
                """)
            .Produces<List<SeatDto>>(StatusCodes.Status200OK)
            .RequireRateLimiting(RateLimitPolicies.Read);

        group.MapPost("/events/{id:guid}/tickets/generate", async (
            Guid id,
            IEventSeatMapReader seatMapReader,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var seatMapResult = await seatMapReader.GetSeatMapByEventIdAsync(id, cancellationToken);
            if (seatMapResult.IsFailure)
            {
                return Result<GenerateTicketsResponse>.Fail(seatMapResult.Error!).ToHttpResult(StatusCodes.Status201Created);
            }

            var result = await mediator.Send(new GenerateTicketsCommand(id, seatMapResult.Value!), cancellationToken);
            return result.ToHttpResult(StatusCodes.Status201Created);
        })
            .WithName("GenerateTickets")
            .WithSummary("Event biletlerini uretir")
            .WithDescription("""
                Event seat map bilgisinden ticket kayitlarini uretir.
                Event moduluyle direkt referans yerine Core seat map reader contract'i kullanilir.
                """)
            .Produces<GenerateTicketsResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity);

        return app;
    }
}
