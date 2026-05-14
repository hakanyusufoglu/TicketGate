namespace TicketGate.Booking.Features.Tickets.Queries.GetTicketById;

/// <summary>Bilet detay bilgisi. Koltuk, fiyat ve guncel durum icerir.</summary>
public sealed record TicketDetailDto(
    Guid Id,
    Guid EventId,
    string Seat,
    decimal Price,
    string Status,
    DateTime CreatedAt);
