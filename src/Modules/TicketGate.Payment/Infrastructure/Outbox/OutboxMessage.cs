using System.Text.Json;

namespace TicketGate.Payment.Infrastructure.Outbox;

/// <summary>
/// Outbox mesaj varligi. Uygulama ve harici servis arasindaki atomikligi saglar.
/// Payment ve OutboxMessage tek transaction'da yazilir; OutboxWorker bu tabloyu polling ile okur.
/// </summary>
public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; } = default!;

    public string Payload { get; private set; } = default!;

    public DateTime CreatedAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public string? Error { get; private set; }

    public int RetryCount { get; private set; }

    /// <summary>
    /// Yeni outbox mesaji olusturur.
    /// Type event tipini, Payload JSON serialize edilmis event icerigini tasir.
    /// </summary>
    public static OutboxMessage Create(string type, object payload)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Basarili islem sonrasi ProcessedAt doldurulur.
    /// OutboxWorker bu mesaji bir daha islemez.
    /// </summary>
    public void MarkProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
        Error = null;
    }

    /// <summary>
    /// Basarisiz islem sonrasi RetryCount artirilir ve hata mesaji kaydedilir.
    /// Worker retry kararini OutboxSettings.MaxRetryCount degeriyle verir.
    /// </summary>
    public void MarkFailed(string error)
    {
        RetryCount++;
        Error = error;
    }

    /// <summary>
    /// RetryCount MaxRetryCount'a ulastiginda Dead Letter durumunu hesaplar.
    /// ProcessedAt doluysa mesaj zaten basariyla tamamlanmistir.
    /// </summary>
    public bool IsDeadLetter(int maxRetryCount)
    {
        return RetryCount >= maxRetryCount && ProcessedAt is null;
    }
}
