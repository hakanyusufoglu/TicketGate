namespace TicketGate.Notification.Configuration;

/// <summary>
/// SSE bildirim akisi yapilandirma ayarlari.
/// Heartbeat araligi appsettings uzerinden yonetilir ve kodda sabit sayi olarak tutulmaz.
/// </summary>
public sealed class SseSettings
{
    public const string SectionName = "SseSettings";

    public int HeartbeatIntervalSeconds { get; init; } = 15;
}
