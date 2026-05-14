using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketGate.Booking.Domain.Entities;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.Tickets.Commands.GenerateTickets;

/// <summary>
/// Event'in venue seat map'ine gore ticket'lari generate eder.
/// Her section, row ve seat icin Ticket.Create cagrilir; fiyat ilgili section'dan alinir.
/// Daha once generate edilmisse 409 doner; komut idempotent degildir.
/// </summary>
internal sealed class GenerateTicketsHandler(BookingDbContext db)
    : IRequestHandler<GenerateTicketsCommand, Result<GenerateTicketsResponse>>
{
    /// <summary>
    /// Akis: daha once generate edildi mi kontrol eder, SeatMap uzerinden ticket listesi olusturur ve bulk insert yapar.
    /// Tekrar generate race condition riski unique index olmadigi icin ileride DB constraint ile guclendirilmelidir.
    /// </summary>
    public async Task<Result<GenerateTicketsResponse>> Handle(
        GenerateTicketsCommand request,
        CancellationToken cancellationToken)
    {
        var exists = await db.Tickets
            .AnyAsync(ticket => ticket.EventId == request.EventId, cancellationToken);

        if (exists)
        {
            return Result<GenerateTicketsResponse>.Fail(
                AppError.Conflict(
                    "Tickets.AlreadyGenerated",
                    "Tickets already generated for this event."));
        }

        var tickets = new List<Ticket>(request.SeatMap.TotalCapacity);
        foreach (var section in request.SeatMap.Sections)
        {
            foreach (var row in section.Rows)
            {
                foreach (var seat in row.Seats)
                {
                    var seatCode = $"{section.Id}-{row.RowCode}-{seat}";
                    tickets.Add(Ticket.Create(request.EventId, seatCode, section.Price));
                }
            }
        }

        await db.Tickets.AddRangeAsync(tickets, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result<GenerateTicketsResponse>.Ok(new GenerateTicketsResponse(tickets.Count));
    }
}
