namespace TicketGate.Payment.Features.Payments.Commands.InitiatePayment;

/// <summary>Odeme baslatma yaniti.</summary>
public sealed record InitiatePaymentResponse(Guid PaymentId, string Status);
