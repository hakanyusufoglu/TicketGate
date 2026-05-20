using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace TicketGate.Booking.Infrastructure.Services;

/// <summary>
/// Active checkout sayaci Redis implementasyonu.
/// INCR/DECR operasyonlari kullanici sahipligi set'i ile atomik yonetilir.
/// Lua scriptler duplicate grant, duplicate release ve negatif sayac riskini engeller.
/// </summary>
public sealed class ActiveCheckoutService(
    IConnectionMultiplexer redis,
    ILogger<ActiveCheckoutService> logger) : IActiveCheckoutService
{
    private const string ActiveCheckoutPrefix = "active_checkout:";
    private const string ActiveCheckoutUsersPrefix = "active_checkout_users:";
    private const string WaitingRoomPrefix = "waitingroom:";

    /// <summary>
    /// Kullaniciyi active checkout sahiplik set'ine ekler ve yeni sahiplikse sayaci artirir.
    /// SADD ve INCR tek Lua scriptte oldugu icin retry durumunda sayac cift artmaz.
    /// </summary>
    public async Task IncrementAsync(Guid eventId, Guid userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        const string script = """
            local added = redis.call("SADD", KEYS[2], ARGV[1])
            if added == 1 then
                return redis.call("INCR", KEYS[1])
            end
            return tonumber(redis.call("GET", KEYS[1]) or "0")
            """;

        await redis.GetDatabase().ScriptEvaluateAsync(
            script,
            [ToActiveCheckoutKey(eventId), ToActiveCheckoutUsersKey(eventId)],
            [userId.ToString()]);

        logger.LogDebug(
            "Active checkout increment requested: {EventId} User: {UserId}",
            eventId,
            userId);
    }

    /// <summary>
    /// Kapasite uygunsa kullaniciyi active checkout sahiplik set'ine ekler ve sayaci artirir.
    /// Kullanici zaten aktif slot sahibiyse idempotent olarak basarili kabul edilir.
    /// </summary>
    public async Task<bool> TryIncrementWithinCapacityAsync(
        Guid eventId,
        Guid userId,
        int maxCapacity,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        const string script = """
            if redis.call("SISMEMBER", KEYS[2], ARGV[1]) == 1 then
                return 1
            end

            local current = tonumber(redis.call("GET", KEYS[1]) or "0")
            local max = tonumber(ARGV[2])
            if current < max then
                redis.call("SADD", KEYS[2], ARGV[1])
                redis.call("INCR", KEYS[1])
                return 1
            end

            return 0
            """;

        var result = (int)await redis.GetDatabase().ScriptEvaluateAsync(
            script,
            [ToActiveCheckoutKey(eventId), ToActiveCheckoutUsersKey(eventId)],
            [userId.ToString(), maxCapacity]);

        return result == 1;
    }

    /// <summary>
    /// Waiting room'dan bos kapasite kadar kullaniciyi cikarir ve checkout sahipligini verir.
    /// ZPOPMIN, SADD ve INCR ayni Lua scriptinde oldugu icin coklu dispatcher kapasiteyi asamaz.
    /// </summary>
    public async Task<IReadOnlyCollection<Guid>> GrantQueuedUsersAsync(
        Guid eventId,
        int maxCapacity,
        int batchSize,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        const string script = """
            local current = tonumber(redis.call("GET", KEYS[2]) or "0")
            local max = tonumber(ARGV[1])
            local batch = tonumber(ARGV[2])
            local capacity = max - current
            if capacity <= 0 then
                return {}
            end

            local limit = math.min(capacity, batch)
            local granted = {}
            for i = 1, limit do
                local popped = redis.call("ZPOPMIN", KEYS[1], 1)
                if #popped == 0 then
                    break
                end

                local userId = popped[1]
                local added = redis.call("SADD", KEYS[3], userId)
                if added == 1 then
                    redis.call("INCR", KEYS[2])
                    table.insert(granted, userId)
                end
            end

            return granted
            """;

        var result = (RedisResult[]?)await redis.GetDatabase().ScriptEvaluateAsync(
            script,
            [ToWaitingRoomKey(eventId), ToActiveCheckoutKey(eventId), ToActiveCheckoutUsersKey(eventId)],
            [maxCapacity, batchSize]);

        return result?
            .Select(item => item.ToString())
            .Where(value => Guid.TryParse(value, out _))
            .Select(Guid.Parse)
            .ToArray() ?? [];
    }

    /// <summary>
    /// Kullanici active checkout sahiplik set'indeyse sahipligi kaldirir ve sayaci dusurur.
    /// SREM ve kosullu DECR tek Lua scriptte oldugu icin tekrar gelen cikis event'i sayaci negatife indirmez.
    /// </summary>
    public async Task<bool> DecrementAsync(Guid eventId, Guid userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        const string script = """
            local removed = redis.call("SREM", KEYS[2], ARGV[1])
            if removed == 1 then
                local current = tonumber(redis.call("GET", KEYS[1]) or "0")
                if current > 0 then
                    redis.call("DECR", KEYS[1])
                    return 1
                end
            end

            return 0
            """;

        var result = (int)await redis.GetDatabase().ScriptEvaluateAsync(
            script,
            [ToActiveCheckoutKey(eventId), ToActiveCheckoutUsersKey(eventId)],
            [userId.ToString()]);

        logger.LogDebug(
            "Active checkout decrement requested: {EventId} User: {UserId}",
            eventId,
            userId);

        return result == 1;
    }

    /// <summary>
    /// Redis active_checkout sayacini okur.
    /// Anahtar yoksa veya deger bos ise sifir dondurulur.
    /// </summary>
    public async Task<long> GetCountAsync(Guid eventId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var value = await redis.GetDatabase().StringGetAsync(ToActiveCheckoutKey(eventId));
        return value.HasValue ? (long)value : 0;
    }

    /// <summary>Event id icin aktif checkout sayaci anahtarini uretir.</summary>
    private static RedisKey ToActiveCheckoutKey(Guid eventId)
    {
        return $"{ActiveCheckoutPrefix}{eventId}";
    }

    /// <summary>Event id icin aktif checkout kullanici set'i anahtarini uretir.</summary>
    private static RedisKey ToActiveCheckoutUsersKey(Guid eventId)
    {
        return $"{ActiveCheckoutUsersPrefix}{eventId}";
    }

    /// <summary>Event id icin waiting room Sorted Set anahtarini uretir.</summary>
    private static RedisKey ToWaitingRoomKey(Guid eventId)
    {
        return $"{WaitingRoomPrefix}{eventId}";
    }
}
