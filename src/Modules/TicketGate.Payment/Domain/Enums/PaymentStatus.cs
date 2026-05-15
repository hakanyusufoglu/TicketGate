namespace TicketGate.Payment.Domain.Enums;

/// <summary>
/// Odeme durum gecislerini tanimlar.
/// Pending outbox worker bekler, Completed basarili gateway sonucudur, Failed retry limiti sonrasi kullanilir, Refunded iade tamamlaninca set edilir.
/// </summary>
public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}
