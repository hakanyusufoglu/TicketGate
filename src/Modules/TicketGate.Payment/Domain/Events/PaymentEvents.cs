using TicketGate.Core.Events;

namespace TicketGate.Payment.Domain.Events;

/// <summary>Odeme basariyla tamamlandiginda yayinlanan domain event.</summary>
public sealed record PaymentCompleted(Guid PaymentId, Guid TicketId, Guid UserId) : DomainEvent;

/// <summary>Odeme basarisiz oldugunda yayinlanan domain event.</summary>
public sealed record PaymentFailed(Guid PaymentId, Guid TicketId, Guid UserId) : DomainEvent;

/// <summary>Iade tamamlandiginda yayinlanan domain event.</summary>
public sealed record PaymentRefunded(Guid PaymentId, Guid TicketId, Guid UserId) : DomainEvent;
