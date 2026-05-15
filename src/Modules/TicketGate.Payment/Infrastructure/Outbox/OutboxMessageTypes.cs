namespace TicketGate.Payment.Infrastructure.Outbox;

/// <summary>
/// Payment outbox mesaj tiplerini merkezi tutar.
/// Magic string kullanimi yerine handler ve testler ayni sabitleri paylasir.
/// </summary>
public static class OutboxMessageTypes
{
    public const string PaymentInitiated = "payment.initiated";

    public const string PaymentRefundRequested = "payment.refund_requested";
}
