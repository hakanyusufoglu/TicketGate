using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TicketGate.Core.Domain;
using TicketGate.Event.Configuration;
using TicketGate.Event.Features.Events.Queries.GetEventById;

namespace TicketGate.Event.Infrastructure.Cache;

/// <summary>
/// Event modulu Redis cache servisidir.
/// Sik okunan, seyrek degisen event detayi ve venue seat map verilerini cache-aside pattern ile saklar.
/// Cache hatalari kritik sayilmaz; handler'lar Postgres okumasina fallback yapar.
/// </summary>
public sealed class EventCacheService(
    IConnectionMultiplexer redis,
    IOptions<EventCacheSettings> settings,
    ILogger<EventCacheService> logger) : IEventCacheService
{
    private readonly EventCacheSettings _settings = settings.Value;

    /// <summary>
    /// Event detayini Redis cache'den okur.
    /// Cache miss veya Redis/serialization hatasinda null donerek DB fallback akisini korur.
    /// </summary>
    public async Task<EventDetailDto?> GetEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        try
        {
            var db = redis.GetDatabase();
            var cached = await db.StringGetAsync(ToEventDetailKey(eventId)).WaitAsync(cancellationToken);

            if (cached.IsNull)
            {
                return null;
            }

            return JsonSerializer.Deserialize<EventDetailDto>((string)cached!);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache read failed for event {EventId}", eventId);
            return null;
        }
    }

    /// <summary>
    /// Event detayini Redis cache'e yazar.
    /// TTL EventCacheSettings uzerinden okunur; cache yazma hatasi ana sorgu sonucunu bozmaz.
    /// </summary>
    public async Task SetEventAsync(Guid eventId, EventDetailDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.StringSetAsync(
                    ToEventDetailKey(eventId),
                    JsonSerializer.Serialize(dto),
                    TimeSpan.FromMinutes(_settings.EventDetailTtlMinutes))
                .WaitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache write failed for event {EventId}", eventId);
        }
    }

    /// <summary>
    /// Venue seat map bilgisini Redis cache'den okur.
    /// Cache miss veya cache hatasinda null doner; caller DB/projection fallback uygulayabilir.
    /// </summary>
    public async Task<SeatMap?> GetSeatMapAsync(Guid venueId, CancellationToken cancellationToken)
    {
        try
        {
            var db = redis.GetDatabase();
            var cached = await db.StringGetAsync(ToSeatMapKey(venueId)).WaitAsync(cancellationToken);

            if (cached.IsNull)
            {
                return null;
            }

            return JsonSerializer.Deserialize<SeatMap>((string)cached!);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache read failed for venue seat map {VenueId}", venueId);
            return null;
        }
    }

    /// <summary>
    /// Venue seat map bilgisini Redis cache'e yazar.
    /// TTL EventCacheSettings uzerinden okunur; cache yazma hatasi request sonucunu etkilemez.
    /// </summary>
    public async Task SetSeatMapAsync(Guid venueId, SeatMap seatMap, CancellationToken cancellationToken)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.StringSetAsync(
                    ToSeatMapKey(venueId),
                    JsonSerializer.Serialize(seatMap),
                    TimeSpan.FromMinutes(_settings.SeatMapTtlMinutes))
                .WaitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache write failed for venue seat map {VenueId}", venueId);
        }
    }

    /// <summary>
    /// Event detay cache kaydini siler.
    /// Event guncelleme ve publish akislarinda stale response riskini temizler.
    /// </summary>
    public async Task InvalidateEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(ToEventDetailKey(eventId)).WaitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache invalidation failed for event {EventId}", eventId);
        }
    }

    /// <summary>Event detay cache anahtarini event:{id}:detail formatinda uretir.</summary>
    private static RedisKey ToEventDetailKey(Guid eventId)
    {
        return new RedisKey($"event:{eventId}:detail");
    }

    /// <summary>Venue seat map cache anahtarini venue:{id}:seatmap formatinda uretir.</summary>
    private static RedisKey ToSeatMapKey(Guid venueId)
    {
        return new RedisKey($"venue:{venueId}:seatmap");
    }
}
