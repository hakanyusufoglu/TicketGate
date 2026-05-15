namespace TicketGate.Notification.Domain;

/// <summary>
/// SSE payload'larinda kullanilan durum sabitleri.
/// Koltuk state string'leri tek noktadan yonetilerek magic string yayilmasi engellenir.
/// </summary>
public static class SsePayloadStatuses
{
    /// <summary>Bilet Redis lock ile rezerve edildi.</summary>
    public const string Reserved = "reserved";

    /// <summary>Bilet tekrar satisa musait hale geldi.</summary>
    public const string Available = "available";

    /// <summary>Bilet odeme sonrasi onaylandi.</summary>
    public const string Confirmed = "confirmed";
}
