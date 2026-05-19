using System.Text.Json;
using Mediator;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TicketGate.Core.Events;
using TicketGate.Notification.Domain;

namespace TicketGate.Notification.Infrastructure;

/// <summary>
/// Entegrasyon event'lerini Redis Pub/Sub kanallarina yayinlar.
/// Mediator INotificationHandler olarak calisir ve SSE endpoint'leri bu kanallari dinleyerek client'a iletir.
/// Her event icin payload tipi sabit tutulur; publish hatalari loglanir ve event akisinin geri kalanini dusurmez.
/// </summary>
public sealed class SsePublisher(
    IConnectionMultiplexer redis,
    ILogger<SsePublisher> logger)
    :
    INotificationHandler<TicketReserved>,
    INotificationHandler<TicketReleased>,
    INotificationHandler<TicketConfirmed>,
    INotificationHandler<QueueTurnGranted>,
    INotificationHandler<UserJoinedQueue>,
    INotificationHandler<QueuePositionChanged>,
    INotificationHandler<PaymentCompleted>
{
    /// <summary>
    /// Bilet rezerve edildiginde seat_status_changed payload'u yayinlar.
    /// Redis Pub/Sub fan-out coklu API instance senaryosunda tum ilgili SSE baglantilarina ulasir.
    /// </summary>
    public async ValueTask Handle(TicketReserved notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = JsonSerializer.Serialize(new
        {
            type = SseEventTypes.SeatStatusChanged,
            ticketId = notification.TicketId,
            seat = notification.Seat,
            status = SsePayloadStatuses.Reserved
        });

        await PublishAsync(SseChannels.SeatStatus(notification.TicketId), payload, cancellationToken);
    }

    /// <summary>
    /// Bilet serbest birakildiginda seat_status_changed payload'u yayinlar.
    /// TTL expire, odeme basarisizligi ve iade akislarinda koltuk UI'i tekrar available durumuna cekilir.
    /// </summary>
    public async ValueTask Handle(TicketReleased notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = JsonSerializer.Serialize(new
        {
            type = SseEventTypes.SeatStatusChanged,
            ticketId = notification.TicketId,
            status = SsePayloadStatuses.Available
        });

        await PublishAsync(SseChannels.SeatStatus(notification.TicketId), payload, cancellationToken);
    }

    /// <summary>
    /// Bilet onaylandiginda seat_status_changed payload'u yayinlar.
    /// Odeme bildirimi PaymentCompleted event'inden uretildigi icin kullaniciya cift payment_confirmed gonderilmez.
    /// </summary>
    public async ValueTask Handle(TicketConfirmed notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = JsonSerializer.Serialize(new
        {
            type = SseEventTypes.SeatStatusChanged,
            ticketId = notification.TicketId,
            status = SsePayloadStatuses.Confirmed
        });

        await PublishAsync(SseChannels.SeatStatus(notification.TicketId), payload, cancellationToken);
    }

    /// <summary>
    /// Waiting room sirasi gelen kullaniciya your_turn payload'u yayinlar.
    /// Kullaniciya ozel kanal kullanildigi icin sira bilgisi baska kullanicilara acilmaz.
    /// </summary>
    public async ValueTask Handle(QueueTurnGranted notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = JsonSerializer.Serialize(new
        {
            type = SseEventTypes.YourTurn,
            eventId = notification.SourceEventId,
            userId = notification.UserId,
            position = notification.Position
        });

        await PublishAsync(SseChannels.QueueTurn(notification.UserId), payload, cancellationToken);
    }

    /// <summary>
    /// Kuyruga giren kullaniciya queue_position payload'u yayinlar.
    /// Toplam kuyruk uzunlugu Redis Sorted Set'ten okunur; Pub/Sub mesaji kalici olmadigi icin sadece anlik UI guncellemesi saglar.
    /// </summary>
    public async ValueTask Handle(UserJoinedQueue notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var total = await db.SortedSetLengthAsync($"waitingroom:{notification.SourceEventId}");
        await PublishPositionAsync(
            notification.UserId,
            notification.SourceEventId,
            notification.Position,
            total,
            cancellationToken);
    }

    /// <summary>
    /// Waiting room sirasi degisen kullaniciya queue_position payload'u yayinlar.
    /// Dispatcher her turdan sonra kalan kullanicilar icin bu event'i uretir.
    /// </summary>
    public async ValueTask Handle(QueuePositionChanged notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await PublishPositionAsync(
            notification.UserId,
            notification.SourceEventId,
            notification.Position,
            notification.Total,
            cancellationToken);
    }

    /// <summary>
    /// Odeme tamamlandiginda kullaniciya payment_confirmed payload'u yayinlar.
    /// PaymentCompleted entegrasyon event'i kaynak kabul edilir; bilet onay event'i ayrica odeme bildirimi uretmez.
    /// </summary>
    public async ValueTask Handle(PaymentCompleted notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = JsonSerializer.Serialize(new
        {
            type = SseEventTypes.PaymentConfirmed,
            paymentId = notification.PaymentId,
            ticketId = notification.TicketId,
            userId = notification.UserId
        });

        await PublishAsync(SseChannels.PaymentConfirmed(notification.UserId), payload, cancellationToken);
    }

    /// <summary>
    /// Queue position payload'unu kullaniciya ozel kanala yayinlar.
    /// Ayrica QueuePositionPublisher ile ayni payload contract'ini korur.
    /// </summary>
    private async Task PublishPositionAsync(
        Guid userId,
        Guid eventId,
        long position,
        long total,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type = SseEventTypes.QueuePosition,
            eventId,
            userId,
            position,
            total
        });

        await PublishAsync(SseChannels.QueueTurn(userId), payload, cancellationToken);
    }

    /// <summary>
    /// Redis PUBLISH islemini gerceklestirir.
    /// Hata durumunda structured log yazar; exception firlatmadigi icin asil domain event zincirini kirmaz.
    /// </summary>
    private async Task PublishAsync(string channel, string payload, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var db = redis.GetDatabase();
            await db.PublishAsync(new RedisChannel(channel, RedisChannel.PatternMode.Literal), payload);
            logger.LogDebug("SSE event published to Redis channel {Channel}", channel);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SSE publish failed for Redis channel {Channel}", channel);
        }
    }
}
