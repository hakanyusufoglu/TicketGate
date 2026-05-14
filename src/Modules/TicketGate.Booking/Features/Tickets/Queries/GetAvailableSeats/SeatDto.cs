namespace TicketGate.Booking.Features.Tickets.Queries.GetAvailableSeats;

/// <summary>
/// Musait koltuk ozet bilgisi.
/// Section, row ve seat bilgisi ile fiyati icerir.
/// </summary>
public sealed record SeatDto(
    Guid TicketId,
    string SeatCode,
    string Section,
    string Row,
    int SeatNumber,
    decimal Price);
