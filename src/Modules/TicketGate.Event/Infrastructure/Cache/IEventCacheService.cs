using TicketGate.Core.Domain;
using TicketGate.Event.Features.Events.Queries.GetEventById;

namespace TicketGate.Event.Infrastructure.Cache;

/// <summary>
/// Event modulu Redis cache soyutlamasidir.
/// Handler'lar cache hatalarini bilmeden cache-aside akisini surdurur.
/// </summary>
public interface IEventCacheService
{
    /// <summary>Event detayini cache'den okur; cache miss veya hata durumunda null doner.</summary>
    Task<EventDetailDto?> GetEventAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Event detayini ayarlarla belirlenen TTL boyunca cache'e yazar.</summary>
    Task SetEventAsync(Guid eventId, EventDetailDto dto, CancellationToken cancellationToken);

    /// <summary>Venue seat map bilgisini cache'den okur; cache miss veya hata durumunda null doner.</summary>
    Task<SeatMap?> GetSeatMapAsync(Guid venueId, CancellationToken cancellationToken);

    /// <summary>Venue seat map bilgisini ayarlarla belirlenen TTL boyunca cache'e yazar.</summary>
    Task SetSeatMapAsync(Guid venueId, SeatMap seatMap, CancellationToken cancellationToken);

    /// <summary>Event guncellenince veya yayinlaninca stale detail cache kaydini temizler.</summary>
    Task InvalidateEventAsync(Guid eventId, CancellationToken cancellationToken);
}

