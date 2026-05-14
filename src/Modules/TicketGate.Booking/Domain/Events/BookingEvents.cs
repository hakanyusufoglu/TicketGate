using TicketGate.Core.Events;

namespace TicketGate.Booking.Domain.Events;

/// <summary>Bilet rezerve edildiginde yayinlanan domain event.</summary>
public sealed record TicketReserved(
    Guid TicketId,
    Guid SourceEventId,
    string Seat,
    decimal Price,
    Guid UserId,
    DateTime ExpiresAt) : DomainEvent;

/// <summary>Odeme tamamlanip bilet onaylandiginda yayinlanan domain event.</summary>
public sealed record TicketConfirmed(
    Guid TicketId,
    Guid SourceEventId,
    Guid UserId) : DomainEvent;

/// <summary>
/// TTL expire veya vazgecme sonucu kilit kaldirildiginda yayinlanan event.
/// Notification modulu bu eventi dinleyerek SSE uzerinden bildirim gonderir.
/// </summary>
public sealed record TicketReleased(
    Guid TicketId,
    Guid SourceEventId,
    Guid? UserId) : DomainEvent;

/// <summary>Bilet iptal edildiginde yayinlanan domain event.</summary>
public sealed record TicketCancelled(
    Guid TicketId,
    Guid SourceEventId,
    Guid UserId) : DomainEvent;
