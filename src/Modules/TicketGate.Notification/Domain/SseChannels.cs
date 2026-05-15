namespace TicketGate.Notification.Domain;

/// <summary>
/// Redis Pub/Sub kanal isimleri. SSE endpoint'leri bu kanallari dinler.
/// SsePublisher domain event gelince ilgili kanala PUBLISH yapar.
/// </summary>
public static class SseChannels
{
    /// <summary>
    /// Koltuk durum degisikligi kanalini uretir.
    /// Her ticket icin ayri kanal kullanildigi icin yalnizca ilgili client tetiklenir.
    /// </summary>
    public static string SeatStatus(Guid ticketId)
    {
        return $"seat:{ticketId}:status";
    }

    /// <summary>
    /// Waiting room sira ve pozisyon bildirim kanalini uretir.
    /// Her kullanici icin ayri kanal kullanildigi icin veri sizintisi riski azalir.
    /// </summary>
    public static string QueueTurn(Guid userId)
    {
        return $"queue:{userId}:turn";
    }

    /// <summary>
    /// Odeme tamamlandi bildirim kanalini uretir.
    /// Kullaniciya ozel kanal odeme durumunun baska client'a gitmesini engeller.
    /// </summary>
    public static string PaymentConfirmed(Guid userId)
    {
        return $"payment:{userId}:confirmed";
    }
}
