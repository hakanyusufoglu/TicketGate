using System.Text.Json;
using Mediator;
using StackExchange.Redis;
using TicketGate.Core.Events;

namespace TicketGate.Booking.Tests.Infrastructure;

/// <summary>
/// Booking dispatcher testleri icin QueueTurnGranted event'ini Redis Pub/Sub mesajina cevirir.
/// Production'da bu sorumluluk Notification modulu SsePublisher'dadir; test burada sadece event publish akisini dogrular.
/// </summary>
public sealed class QueueTurnGrantedRedisHandler(IConnectionMultiplexer redis)
    :
    INotificationHandler<QueueTurnGranted>,
    INotificationHandler<QueuePositionChanged>
{
    private const string YourTurnEventType = "your_turn";
    private const string QueuePositionEventType = "queue_position";

    /// <summary>
    /// QueueTurnGranted event'i geldigi anda kullaniciya ozel Redis kanalina your_turn payload'u yayinlar.
    /// Dispatcher'in dogrudan Redis'e bagli kalmadigini, Mediator event siniri uzerinden calistigini test eder.
    /// </summary>
    public async ValueTask Handle(QueueTurnGranted notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = JsonSerializer.Serialize(new
        {
            type = YourTurnEventType,
            eventId = notification.SourceEventId,
            userId = notification.UserId,
            position = notification.Position
        });

        await redis.GetDatabase().PublishAsync(
            new RedisChannel($"queue:{notification.UserId}:turn", RedisChannel.PatternMode.Literal),
            payload);
    }

    /// <summary>
    /// QueuePositionChanged event'i geldigi anda kullaniciya ozel Redis kanalina queue_position payload'u yayinlar.
    /// Dispatcher turu sonrasi kalan kullanicilarin yeni sirasini test eder.
    /// </summary>
    public async ValueTask Handle(QueuePositionChanged notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = JsonSerializer.Serialize(new
        {
            type = QueuePositionEventType,
            eventId = notification.SourceEventId,
            userId = notification.UserId,
            position = notification.Position,
            total = notification.Total
        });

        await redis.GetDatabase().PublishAsync(
            new RedisChannel($"queue:{notification.UserId}:turn", RedisChannel.PatternMode.Literal),
            payload);
    }
}
