namespace TicketGate.Payment.Infrastructure.Workers.Payloads;

/// <summary>
/// PaymentInitiated outbox mesajinin payload modelidir.
/// OutboxWorker bu veriyi deserialize ederek MockPaymentGateway veya production gateway'e charge istegi tasir.
/// </summary>
public sealed record PaymentInitiatedOutboxPayload(
    Guid PaymentId,
    Guid TicketId,
    Guid UserId,
    decimal Amount,
    string Currency,
    string Provider);
