namespace TicketGate.Booking.Features.Tickets.Queries.GetAvailableSeats;

/// <summary>Koltuk ozet bilgisi. Sadece Available durumdaki biletler doner.</summary>
public sealed record SeatDto(Guid TicketId, string Seat, decimal Price);
