namespace TicketGate.Event.Configuration;

/// <summary>
/// Event modulu cache ayarlarini tasir.
/// Event detayi, seat map ve output cache TTL degerleri appsettings uzerinden okunur.
/// </summary>
public sealed class EventCacheSettings
{
    public const string SectionName = "EventCacheSettings";

    public int EventDetailTtlMinutes { get; init; } = 10;

    public int SeatMapTtlMinutes { get; init; } = 30;

    public int EventListOutputCacheTtlSeconds { get; init; } = 60;
}

