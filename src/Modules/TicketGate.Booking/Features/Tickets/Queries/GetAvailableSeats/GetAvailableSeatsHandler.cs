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
        var tickets = await db.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.EventId == request.EventId && ticket.Status == TicketStatus.Available)
            .OrderBy(ticket => ticket.Seat)
            .Select(ticket => new
            {
                ticket.Id,
                ticket.Seat,
                ticket.Price
            })
            .ToListAsync(cancellationToken);

        var seats = tickets
            .Select(ticket => ToSeatDto(ticket.Id, ticket.Seat, ticket.Price))
            .ToList();

        return Result<List<SeatDto>>.Ok(seats);
    }

    /// <summary>
    /// SeatCode bilgisini Section, Row ve SeatNumber alanlarina ayirir.
    /// Eski A-1 formatli seed/test biletleri icin section bos birakilarak geriye uyumluluk korunur.
    /// </summary>
    private static SeatDto ToSeatDto(Guid ticketId, string seatCode, decimal price)
    {
        var parts = seatCode.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 3 && int.TryParse(parts[2], out var sectionSeatNumber))
        {
            return new SeatDto(ticketId, seatCode, parts[0], parts[1], sectionSeatNumber, price);
        }

        if (parts.Length == 2 && int.TryParse(parts[1], out var rowSeatNumber))
        {
            return new SeatDto(ticketId, seatCode, string.Empty, parts[0], rowSeatNumber, price);
        }

        return new SeatDto(ticketId, seatCode, string.Empty, string.Empty, 0, price);
    }
}
