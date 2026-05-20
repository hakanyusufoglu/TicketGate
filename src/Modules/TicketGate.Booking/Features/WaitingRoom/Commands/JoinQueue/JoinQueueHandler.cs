using Mediator;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TicketGate.Booking.Configuration;
using TicketGate.Booking.Infrastructure.Services;
using TicketGate.Core.Events;
using TicketGate.Core.Metrics;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.WaitingRoom.Commands.JoinQueue;

/// <summary>
/// Kullaniciyi virtual waiting room kuyruguna ekler.
/// Kapasite bos ise active checkout sayacini atomik artirip direkt gecis verir; doluysa Redis ZADD NX ile sirayi korur.
/// NX flag ayni kullanicinin iki kez eklenmesini engeller ve ilk giris zamanini saklar.
/// </summary>
public sealed class JoinQueueHandler(
    IConnectionMultiplexer redis,
    IActiveCheckoutService activeCheckoutService,
    IOptions<BookingSettings> settings,
    IMediator mediator) : IRequestHandler<JoinQueueCommand, Result<JoinQueueResponse>>
{
    /// <summary>
    /// Akis: kapasite kontrolu, bos ise direkt grant, dolu ise ZADD NX, ZRANK ve UserJoinedQueue event.
    /// active_checkout:{eventId} sayaci MaxCheckoutCapacity ile karsilastirilir.
    /// </summary>
    public async ValueTask<Result<JoinQueueResponse>> Handle(
        JoinQueueCommand request,
        CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();

        if (await activeCheckoutService.TryIncrementWithinCapacityAsync(
            request.EventId,
            request.UserId,
            settings.Value.MaxCheckoutCapacity,
            cancellationToken))
        {
            TicketGateMetrics.WaitingRoomDepth.WithLabels(request.EventId.ToString()).Set(0);

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

        await mediator.Publish(
            new UserJoinedQueue(request.EventId, request.UserId, position),
            cancellationToken);

        return Result<JoinQueueResponse>.Ok(new JoinQueueResponse(
            request.EventId,
            request.UserId,
            position,
            CanProceedDirectly: false));
    }

    /// <summary>Event id icin waiting room Sorted Set anahtarini uretir.</summary>
    private static RedisKey ToWaitingRoomKey(Guid eventId)
    {
        return $"waitingroom:{eventId}";
    }
}
