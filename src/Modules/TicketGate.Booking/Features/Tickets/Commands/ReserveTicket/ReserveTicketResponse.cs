namespace TicketGate.Booking.Features.Tickets.Commands.ReserveTicket;

/// <summary>Rezervasyon yaniti. ExpiresAt TTL suresine gore hesaplanir.</summary>
public sealed record ReserveTicketResponse(
    Guid TicketId,
    string Seat,
    decimal Price,
    DateTime ExpiresAt);
