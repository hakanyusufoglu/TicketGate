using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TicketGate.Notification.Domain;

namespace TicketGate.Notification.Infrastructure;

/// <summary>
/// Waiting room pozisyon guncellemelerini Redis Pub/Sub uzerinden yayinlar.
/// Queue dispatcher veya ilerideki pozisyon hesaplayicilari bu servisi kullanarak kullaniciya ozel SSE fan-out tetikler.
/// </summary>
public sealed class QueuePositionPublisher(
    IConnectionMultiplexer redis,
    ILogger<QueuePositionPublisher> logger)
{
    /// <summary>
    /// Belirtilen kullaniciya guncel kuyruk pozisyonunu bildirir.
    /// Mesaj queue:{userId}:turn kanalina queue_position tipiyle yayinlanir ve hata durumunda exception yayilmaz.
    /// </summary>
    public async Task PublishPositionAsync(
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

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var db = redis.GetDatabase();
            await db.PublishAsync(
                new RedisChannel(SseChannels.QueueTurn(userId), RedisChannel.PatternMode.Literal),
                payload);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Queue position publish failed for user {UserId}", userId);
        }
    }
}
