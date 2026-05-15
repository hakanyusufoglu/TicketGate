namespace TicketGate.Core.Events;

/// <summary>Bilet rezerve edildiginde yayinlanan entegrasyon event'idir.</summary>
public sealed record TicketReserved(
    Guid TicketId,
    Guid SourceEventId,
    string Seat,
    decimal Price,
    Guid UserId,
    DateTime ExpiresAt) : DomainEvent;

/// <summary>Odeme tamamlanip bilet onaylandiginda yayinlanan entegrasyon event'idir.</summary>
public sealed record TicketConfirmed(
    Guid TicketId,
    Guid SourceEventId,
    Guid UserId) : DomainEvent;

/// <summary>
/// TTL expire, odeme basarisizligi veya iade sonucu kilit kaldirildiginda yayinlanan entegrasyon event'idir.
/// Notification modulu bu event ile koltuk durumunu SSE uzerinden bildirir.
/// </summary>
public sealed record TicketReleased(
    Guid TicketId,
    Guid SourceEventId,
    Guid? UserId) : DomainEvent;

/// <summary>Bilet iptal edildiginde yayinlanan entegrasyon event'idir.</summary>
public sealed record TicketCancelled(
    Guid TicketId,
    Guid SourceEventId,
    Guid UserId) : DomainEvent;

/// <summary>
/// Kullanicinin waiting room sirasi geldiginde yayinlanan entegrasyon event'idir.
/// Notification modulu bu event ile kullaniciya ozel SSE bildirimi uretir.
/// </summary>
public sealed record QueueTurnGranted(
    Guid SourceEventId,
    Guid UserId,
    long Position) : DomainEvent;

/// <summary>
/// Kullanici waiting room kuyruguna katildiginda yayinlanan entegrasyon event'idir.
/// Notification modulu bu event ile guncel queue_position bildirimi uretir.
/// </summary>
public sealed record UserJoinedQueue(
    Guid SourceEventId,
    Guid UserId,
    long Position) : DomainEvent;

/// <summary>
/// Waiting room sirasi degistiginde kullanici icin yayinlanan entegrasyon event'idir.
/// Notification modulu bu event ile queue_position SSE payload'u uretir.
/// </summary>
public sealed record QueuePositionChanged(
    Guid SourceEventId,
    Guid UserId,
    long Position,
    long Total) : DomainEvent;
