using MediatR;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TicketGate.Booking.Configuration;
using TicketGate.Core.Events;
using TicketGate.Core.Metrics;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.WaitingRoom.Commands.JoinQueue;

/// <summary>
/// Kullaniciyi virtual waiting room kuyruguna ekler.
/// Kapasite bos ise active checkout sayacini atomik artirip direkt gecis verir; doluysa Redis ZADD NX ile sirayi korur.
/// NX flag ayni kullanicinin iki kez eklenmesini engeller ve ilk giris zamanini saklar.
/// </summary>
internal sealed class JoinQueueHandler(
    IConnectionMultiplexer redis,
    IOptions<BookingSettings> settings,
    IPublisher publisher) : IRequestHandler<JoinQueueCommand, Result<JoinQueueResponse>>
{
    /// <summary>
    /// Akis: kapasite kontrolu, bos ise direkt grant, dolu ise ZADD NX, ZRANK ve UserJoinedQueue event.
    /// active_checkout:{eventId} sayaci MaxCheckoutCapacity ile karsilastirilir.
    /// </summary>
    public async Task<Result<JoinQueueResponse>> Handle(
        JoinQueueCommand request,
        CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();

        if (await TryGrantDirectCheckoutAsync(db, request.EventId, cancellationToken))
        {
            return Result<JoinQueueResponse>.Ok(new JoinQueueResponse(
                request.EventId,
                request.UserId,
                Position: 0,
                CanProceedDirectly: true));
        }

        var queueKey = ToWaitingRoomKey(request.EventId);
        var score = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await db.SortedSetAddAsync(queueKey, request.UserId.ToString(), score, When.NotExists);
        var queueDepth = await db.SortedSetLengthAsync(queueKey);
        TicketGateMetrics.WaitingRoomDepth.WithLabels(request.EventId.ToString()).Set(queueDepth);

        var rank = await db.SortedSetRankAsync(queueKey, request.UserId.ToString(), Order.Ascending);
        var position = (rank ?? 0) + 1;

        await publisher.Publish(
            new UserJoinedQueue(request.EventId, request.UserId, position),
            cancellationToken);

        return Result<JoinQueueResponse>.Ok(new JoinQueueResponse(
            request.EventId,
            request.UserId,
            position,
            CanProceedDirectly: false));
    }

    /// <summary>
    /// active_checkout:{eventId} Redis sayacini atomik olarak kontrol edip artirir.
    /// Lua script kapasite kontrolu ve INCR'i tek Redis operasyonuna indirerek overbooking race condition'ini engeller.
    /// </summary>
    private async Task<bool> TryGrantDirectCheckoutAsync(
        IDatabase db,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string script = """
            local current = tonumber(redis.call("GET", KEYS[1]) or "0")
            local max = tonumber(ARGV[1])
            if current < max then
                redis.call("INCR", KEYS[1])
                return 1
            end
            return 0
            """;

        var result = (int)await db.ScriptEvaluateAsync(
            script,
            [ToActiveCheckoutKey(eventId)],
            [settings.Value.MaxCheckoutCapacity]);

        return result == 1;
    }

    /// <summary>Event id icin waiting room Sorted Set anahtarini uretir.</summary>
    private static RedisKey ToWaitingRoomKey(Guid eventId)
    {
        return $"waitingroom:{eventId}";
    }

    /// <summary>Event id icin aktif checkout sayaci anahtarini uretir.</summary>
    private static RedisKey ToActiveCheckoutKey(Guid eventId)
    {
        return $"active_checkout:{eventId}";
    }
}
