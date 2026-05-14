using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketGate.Booking.Domain.Enums;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.Tickets.Queries.GetAvailableSeats;

/// <summary>
/// Etkinlige ait Available durumdaki biletleri listeler.
/// AsNoTracking ve Select projection kullanilir; gereksiz kolon yuklenmez.
/// </summary>
internal sealed class GetAvailableSeatsHandler(BookingDbContext db)
    : IRequestHandler<GetAvailableSeatsQuery, Result<List<SeatDto>>>
{
    /// <summary>
    /// EventId ve Available status filtresiyle koltuk listesini okur.
    /// Composite index bu sorgunun event bazli seat taramasini dusuk maliyetli hale getirir.
    /// </summary>
    public async Task<Result<List<SeatDto>>> Handle(
        GetAvailableSeatsQuery request,
        CancellationToken cancellationToken)
    {
        var seats = await db.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.EventId == request.EventId && ticket.Status == TicketStatus.Available)
            .OrderBy(ticket => ticket.Seat)
            .Select(ticket => new SeatDto(ticket.Id, ticket.Seat, ticket.Price))
            .ToListAsync(cancellationToken);

        return Result<List<SeatDto>>.Ok(seats);
    }
}
