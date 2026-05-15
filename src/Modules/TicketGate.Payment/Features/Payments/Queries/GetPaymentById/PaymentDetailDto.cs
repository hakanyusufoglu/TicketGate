namespace TicketGate.Payment.Features.Payments.Queries.GetPaymentById;

/// <summary>Odeme detay bilgisi.</summary>
public sealed record PaymentDetailDto(
    Guid Id,
    Guid TicketId,
    decimal Amount,
    string Currency,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt);
