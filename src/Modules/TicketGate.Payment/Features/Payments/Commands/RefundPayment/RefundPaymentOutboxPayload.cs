namespace TicketGate.Payment.Features.Payments.Commands.RefundPayment;

/// <summary>OutboxWorker'in harici gateway refund islemi icin okuyacagi iade payload'u.</summary>
public sealed record RefundPaymentOutboxPayload(
    Guid PaymentId,
    Guid TicketId,
    Guid UserId,
    string ExternalPaymentId);
