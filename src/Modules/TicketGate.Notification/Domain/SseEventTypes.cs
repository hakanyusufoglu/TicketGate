namespace TicketGate.Notification.Domain;

/// <summary>
/// SSE event tip sabitleri. Client bu tiplere gore
/// gelen mesaji hangi UI elemanina yansitacagini bilir.
/// </summary>
public static class SseEventTypes
{
    /// <summary>Koltuk durumu degisti; rezerve, musait veya onayli.</summary>
    public const string SeatStatusChanged = "seat_status_changed";

    /// <summary>Waiting room sirasi geldi; kullanici rezervasyona gecebilir.</summary>
    public const string YourTurn = "your_turn";

    /// <summary>Odeme tamamlandi; bilet onay akisi basarili ilerledi.</summary>
    public const string PaymentConfirmed = "payment_confirmed";

    /// <summary>Waiting room pozisyon guncellemesi.</summary>
    public const string QueuePosition = "queue_position";
}
