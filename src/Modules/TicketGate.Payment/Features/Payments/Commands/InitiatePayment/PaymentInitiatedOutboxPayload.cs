namespace TicketGate.Payment.Features.Payments.Commands.InitiatePayment;

/// <summary>OutboxWorker'in harici gateway charge islemi icin okuyacagi odeme baslatma payload'u.</summary>
public sealed record PaymentInitiatedOutboxPayload(
    Guid PaymentId,
    Guid TicketId,
    Guid UserId,
    decimal Amount,
    string Currency,
    string Provider);
