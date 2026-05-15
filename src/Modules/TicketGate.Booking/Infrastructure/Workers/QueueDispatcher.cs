using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TicketGate.Booking.Configuration;
using TicketGate.Booking.Domain.Events;

namespace TicketGate.Booking.Infrastructure.Workers;

/// <summary>
/// Waiting room sira yoneticisi. Her ayarli aralikta Redis Sorted Set kuyruklarini tarar.
/// ZPOPMIN ile siradaki kullanicilari alir ve Redis Pub/Sub uzerinden your_turn bildirimi yayinlar.
/// Kapasite MaxCheckoutCapacity - active_checkout sayisi ile hesaplanir; hatalar worker dongusunu dusurmez.
/// </summary>
public sealed class QueueDispatcher(
    IConnectionMultiplexer redis,
    IOptions<BookingSettings> settings,
    IServiceScopeFactory scopeFactory,
    ILogger<QueueDispatcher> logger) : BackgroundService
{
    private const string WaitingRoomPrefix = "waitingroom:";
    private const string ActiveCheckoutPrefix = "active_checkout:";
    private const string QueueTurnEventName = "your_turn";

    /// <summary>
    /// QueueDispatcherIntervalSeconds aralikla calisir.
    /// Aktif kuyrugu olan tum eventleri isler ve hata durumunda donguyu devam ettirir.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllQueuesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "QueueDispatcher error");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(settings.Value.QueueDispatcherIntervalSeconds),
                stoppingToken);
        }
    }

    /// <summary>
    /// Tum aktif waiting room key'lerini tarar.
    /// Her key'den event id parse edilir ve ilgili kuyruk bos kapasiteye gore islenir.
    /// </summary>
    private async Task ProcessAllQueuesAsync(CancellationToken cancellationToken)
    {
        foreach (var endpoint in redis.GetEndPoints())
        {
            var server = redis.GetServer(endpoint);
            foreach (var key in server.Keys(pattern: $"{WaitingRoomPrefix}*"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (TryParseWaitingRoomKey(key.ToString(), out var eventId))
                {
                    await ProcessQueueAsync(eventId, cancellationToken);
                }
            }
        }
    }

    /// <summary>
    /// Bos kapasiteyi hesaplar: MaxCheckoutCapacity - active_checkout.
    /// ZPOPMIN ile batch kadar kullanici alir, her biri icin INCR ve Pub/Sub bildirimi yapar.
    /// </summary>
    private async Task ProcessQueueAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var users = await PopUsersWithinCapacityAsync(db, eventId, cancellationToken);

        if (users.Length == 0)
        {
            return;
        }

        foreach (var userId in users)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await NotifyUserAsync(db, userId, eventId, cancellationToken);
        }
    }

    /// <summary>
    /// Bos kapasite hesaplama, ZPOPMIN ve active_checkout artisini tek Lua script'inde yapar.
    /// Birden fazla dispatcher instance'i calissa bile kapasite asimi ve cift grant race condition'i olusmaz.
    /// </summary>
    private async Task<string[]> PopUsersWithinCapacityAsync(
        IDatabase db,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
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

                redis.call("INCR", KEYS[2])
                table.insert(granted, popped[1])
            end

            return granted
            """;

        var result = (RedisResult[]?)await db.ScriptEvaluateAsync(
            script,
            [ToWaitingRoomKey(eventId), ToActiveCheckoutKey(eventId)],
            [settings.Value.MaxCheckoutCapacity, settings.Value.QueueDispatchBatchSize]);

        return result?.Select(item => item.ToString()).ToArray() ?? [];
    }

    /// <summary>
    /// Redis PUBLISH queue:{userId}:turn kanalina mesaj gonderir.
    /// SSE endpoint P10'da bu kanali dinler; simdilik direkt Pub/Sub kullanilir.
    /// </summary>
    private async Task NotifyUserAsync(
        IDatabase db,
        string userId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = $$"""{"event":"{{QueueTurnEventName}}","eventId":"{{eventId}}","userId":"{{userId}}"}""";
        await db.PublishAsync(new RedisChannel($"queue:{userId}:turn", RedisChannel.PatternMode.Literal), payload);

        if (Guid.TryParse(userId, out var parsedUserId))
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
            await publisher.Publish(new QueueTurnGranted(eventId, parsedUserId, Position: 0), cancellationToken);
        }
    }

    /// <summary>Event id icin waiting room Sorted Set anahtarini uretir.</summary>
    private static RedisKey ToWaitingRoomKey(Guid eventId)
    {
        return $"{WaitingRoomPrefix}{eventId}";
    }

    /// <summary>Event id icin aktif checkout sayaci anahtarini uretir.</summary>
    private static RedisKey ToActiveCheckoutKey(Guid eventId)
    {
        return $"{ActiveCheckoutPrefix}{eventId}";
    }

    /// <summary>
    /// Redis waitingroom:{eventId} key'ini cozumler.
    /// Format uymazsa false doner ve dispatcher key'i atlar.
    /// </summary>
    private static bool TryParseWaitingRoomKey(string key, out Guid eventId)
    {
        eventId = Guid.Empty;
        return key.StartsWith(WaitingRoomPrefix, StringComparison.Ordinal) &&
            Guid.TryParse(key[WaitingRoomPrefix.Length..], out eventId);
    }
}
