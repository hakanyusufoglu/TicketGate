using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TicketGate.Booking.Configuration;
using TicketGate.Booking.Infrastructure.Services;
using TicketGate.Core.Events;
using TicketGate.Core.Metrics;

namespace TicketGate.Booking.Infrastructure.Workers;

/// <summary>
/// Waiting room sira yoneticisi. Her ayarli aralikta Redis Sorted Set kuyruklarini tarar.
/// ZPOPMIN ile siradaki kullanicilari alir ve QueueTurnGranted event'i yayinlar.
/// Kapasite MaxCheckoutCapacity - active_checkout sayisi ile hesaplanir; hatalar worker dongusunu dusurmez.
/// </summary>
public sealed class QueueDispatcher(
    IConnectionMultiplexer redis,
    IActiveCheckoutService activeCheckoutService,
    IOptions<BookingSettings> settings,
    IServiceScopeFactory scopeFactory,
    ILogger<QueueDispatcher> logger) : BackgroundService
{
    private const string WaitingRoomPrefix = "waitingroom:";

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
    /// ZPOPMIN ile batch kadar kullanici alir, her biri icin INCR ve QueueTurnGranted event'i uretir.
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
            await NotifyUserAsync(userId, eventId, cancellationToken);
        }

        await PublishRemainingPositionsAsync(db, eventId, cancellationToken);
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
        var users = await activeCheckoutService.GrantQueuedUsersAsync(
            eventId,
            settings.Value.MaxCheckoutCapacity,
            settings.Value.QueueDispatchBatchSize,
            cancellationToken);

        return users.Select(userId => userId.ToString()).ToArray();
    }

    /// <summary>
    /// Sira verilen kullanici icin QueueTurnGranted entegrasyon event'i yayinlar.
    /// Redis Pub/Sub fan-out Notification modulu tarafindan yapildigi icin Booking modulu kanal formatina baglanmaz.
    /// </summary>
    private async Task NotifyUserAsync(
        string userId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Guid.TryParse(userId, out var parsedUserId))
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Publish(new QueueTurnGranted(eventId, parsedUserId, Position: 0), cancellationToken);
        }
    }

    /// <summary>
    /// Kuyrukta kalan kullanicilarin pozisyon bildirimlerini batch olarak yayinlar.
    /// Tum Sorted Set'i tek seferde bellege almak yerine ayarli batch boyutu kadar okur ve OOM riskini engeller.
    /// Her batch arasinda kisa bekleme ile worker'in diger event'lere firsat vermesini saglar.
    /// </summary>
    private async Task PublishRemainingPositionsAsync(
        IDatabase db,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var waitingRoomKey = ToWaitingRoomKey(eventId);
        var total = await db.SortedSetLengthAsync(waitingRoomKey);
        TicketGateMetrics.WaitingRoomDepth.WithLabels(eventId.ToString()).Set(total);

        if (total == 0)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var batchSize = Math.Max(1, settings.Value.QueuePositionPublishBatchSize);
        var batchDelay = TimeSpan.FromMilliseconds(Math.Max(0, settings.Value.QueuePositionPublishDelayMilliseconds));
        long start = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var batch = await db.SortedSetRangeByRankAsync(
                waitingRoomKey,
                start,
                start + batchSize - 1,
                Order.Ascending);

            if (batch.Length == 0)
            {
                break;
            }

            for (var index = 0; index < batch.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var value = batch[index].ToString();
                if (!Guid.TryParse(value, out var parsedUserId))
                {
                    continue;
                }

                try
                {
                    await mediator.Publish(
                        new QueuePositionChanged(eventId, parsedUserId, Position: start + index + 1, Total: total),
                        cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Pozisyon bildirimi hatasi: {UserId}", value);
                }
            }

            start += batch.Length;

            if (batch.Length < batchSize)
            {
                break;
            }

            await Task.Delay(batchDelay, cancellationToken);
        }
    }

    /// <summary>Event id icin waiting room Sorted Set anahtarini uretir.</summary>
    private static RedisKey ToWaitingRoomKey(Guid eventId)
    {
        return $"{WaitingRoomPrefix}{eventId}";
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
