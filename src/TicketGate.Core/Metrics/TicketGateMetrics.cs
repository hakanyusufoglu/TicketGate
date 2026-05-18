using Prometheus;

namespace TicketGate.Core.Metrics;

/// <summary>
/// TicketGate uygulama metriklerini Prometheus formatinda tanimlar.
/// Moduller API projesine referans veremedigi icin shared kernel uzerinden kullanilir.
/// Grafana dashboard'lari bu metrikleri rezervasyon, kuyruk, odeme ve SSE sagligi icin gorsellestirir.
/// </summary>
public static class TicketGateMetrics
{
    /// <summary>
    /// Toplam ticket rezervasyon sayisini status label'i ile izler.
    /// success, conflict ve not_found ayrimi race condition ve stok tutarliligi analizi icin kritiktir.
    /// </summary>
    public static readonly Counter TicketReservations =
        Prometheus.Metrics.CreateCounter(
            "ticketgate_ticket_reservations_total",
            "Toplam ticket rezervasyon sayisi",
            new CounterConfiguration
            {
                LabelNames = ["status"]
            });

    /// <summary>
    /// Aktif Redis lock sayisini surec icinde gauge olarak izler.
    /// Ani artislar rezervasyon yogunlugu ve checkout darbogazi sinyali verir.
    /// </summary>
    public static readonly Gauge ActiveLocks =
        Prometheus.Metrics.CreateGauge(
            "ticketgate_active_redis_locks",
            "Aktif Redis ticket lock sayisi");

    /// <summary>
    /// Waiting room kuyruk derinligini eventId label'i ile izler.
    /// Yuksek degerler kapasite yetersizligi veya dispatcher yavasligi icin erken uyaridir.
    /// </summary>
    public static readonly Gauge WaitingRoomDepth =
        Prometheus.Metrics.CreateGauge(
            "ticketgate_waiting_room_depth",
            "Waiting room'daki kullanici sayisi",
            new GaugeConfiguration
            {
                LabelNames = ["eventId"]
            });

    /// <summary>
    /// Outbox mesaj isleme suresini saniye cinsinden histogram olarak izler.
    /// p99 degerinin yukselmesi harici odeme gateway'i yavasligi veya baglanti sorunu sinyalidir.
    /// </summary>
    public static readonly Histogram OutboxProcessingDuration =
        Prometheus.Metrics.CreateHistogram(
            "ticketgate_outbox_processing_duration_seconds",
            "Outbox mesaj isleme suresi",
            new HistogramConfiguration
            {
                LabelNames = ["type", "status"]
            });

    /// <summary>
    /// Dead letter'a dusen toplam outbox mesaj sayisini type label'i ile izler.
    /// Sifirdan buyuk her deger odeme akisinda manuel inceleme gerektiren kritik durumdur.
    /// </summary>
    public static readonly Counter OutboxDeadLetters =
        Prometheus.Metrics.CreateCounter(
            "ticketgate_outbox_dead_letters_total",
            "Dead letter olan outbox mesaj sayisi",
            new CounterConfiguration
            {
                LabelNames = ["type"]
            });

    /// <summary>
    /// Aktif SSE baglanti sayisini anlik gauge olarak izler.
    /// Yuksek degerler bellek, socket ve Redis subscription baskisini takip etmek icin kullanilir.
    /// </summary>
    public static readonly Gauge ActiveSseConnections =
        Prometheus.Metrics.CreateGauge(
            "ticketgate_active_sse_connections",
            "Aktif SSE baglanti sayisi");

    /// <summary>
    /// Toplam odeme sonucunu status label'i ile izler.
    /// completed, failed ve refunded ayrimi gelir akisi ve iade oranlarini dashboard'da ayirir.
    /// </summary>
    public static readonly Counter Payments =
        Prometheus.Metrics.CreateCounter(
            "ticketgate_payments_total",
            "Toplam odeme sayisi",
            new CounterConfiguration
            {
                LabelNames = ["status"]
            });
}
