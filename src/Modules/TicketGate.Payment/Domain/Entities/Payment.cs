using TicketGate.Payment.Domain.Enums;

namespace TicketGate.Payment.Domain.Entities;

/// <summary>
/// Odeme varligi. Harici gateway ile iletisim dogrudan bu entity uzerinden yapilmaz; OutboxWorker ustlenir.
/// IdempotencyKey ile ayni odeme isteginin iki kez islenmesi engellenir.
/// </summary>
public sealed class Payment
{
    private Payment()
    {
    }

    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public Guid UserId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = "TRY";

    public PaymentStatus Status { get; private set; }

    public string? ExternalPaymentId { get; private set; }

    public string IdempotencyKey { get; private set; } = default!;

    public DateTime CreatedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Yeni odeme kaydi olusturur. Status baslangicta Pending'dir.
    /// IdempotencyKey benzersiz olmalidir; duplicate kontrolu handler ve unique index ile yapilir.
    /// </summary>
    public static Payment Create(
        Guid ticketId,
        Guid userId,
        decimal amount,
        string idempotencyKey)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UserId = userId,
            Amount = amount,
            Currency = "TRY",
            Status = PaymentStatus.Pending,
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Odemeyi tamamlar. Pending durumundan Completed durumuna gecer.
    /// ExternalPaymentId harici gateway'den donen referanstir ve worker tarafindan yazilir.
    /// </summary>
    public void Complete(string externalPaymentId)
    {
        if (Status != PaymentStatus.Pending)
        {
            return;
        }

        Status = PaymentStatus.Completed;
        ExternalPaymentId = externalPaymentId;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Odemeyi basarisiz isaretler. Pending durumundan Failed durumuna gecer.
    /// OutboxWorker max retry sonrasi bu gecisi tamamlar.
    /// </summary>
    public void Fail()
    {
        if (Status != PaymentStatus.Pending)
        {
            return;
        }

        Status = PaymentStatus.Failed;
    }

    /// <summary>
    /// Iade islemi tamamlandiginda Completed durumundan Refunded durumuna gecer.
    /// Harici iade basarisi OutboxWorker tarafindan dogrulandiktan sonra cagrilmalidir.
    /// </summary>
    public void Refund()
    {
        if (Status != PaymentStatus.Completed)
        {
            return;
        }

        Status = PaymentStatus.Refunded;
    }
}
