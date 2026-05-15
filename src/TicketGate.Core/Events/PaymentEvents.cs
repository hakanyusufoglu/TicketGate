namespace TicketGate.Core.Events;

/// <summary>
/// Odeme basariyla tamamlandiginda yayinlanan entegrasyon event'idir.
/// Booking modulu bu event ile ticket'i Confirmed durumuna alir; moduller arasi direkt proje referansi gerekmez.
/// </summary>
public sealed record PaymentCompleted(Guid PaymentId, Guid TicketId, Guid UserId) : DomainEvent;

/// <summary>
/// Odeme basarisiz olup dead letter durumuna dustugunde yayinlanan entegrasyon event'idir.
/// Booking modulu bu event ile reserved ticket'i tekrar Available durumuna ceker.
/// </summary>
public sealed record PaymentFailed(Guid PaymentId, Guid TicketId, Guid UserId) : DomainEvent;

/// <summary>
/// Odeme iadesi harici gateway tarafinda tamamlandiginda yayinlanan entegrasyon event'idir.
/// Booking ve Notification modulleri iade sonrasindaki yan etkileri bu event uzerinden isleyebilir.
/// </summary>
public sealed record PaymentRefunded(Guid PaymentId, Guid TicketId, Guid UserId) : DomainEvent;
