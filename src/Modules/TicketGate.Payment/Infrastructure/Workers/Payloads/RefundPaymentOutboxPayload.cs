namespace TicketGate.Payment.Infrastructure.Workers.Payloads;

/// <summary>
/// PaymentRefundRequested outbox mesajinin payload modelidir.
/// OutboxWorker bu veriyi deserialize ederek ExternalPaymentId ile refund istegini gateway'e iletir.
/// </summary>
public sealed record RefundPaymentOutboxPayload(
    Guid PaymentId,
    Guid TicketId,
    Guid UserId,
    string ExternalPaymentId);
