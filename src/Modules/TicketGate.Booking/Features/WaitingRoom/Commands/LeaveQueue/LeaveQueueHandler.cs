using Mediator;
using StackExchange.Redis;
using TicketGate.Booking.Infrastructure.Services;
using TicketGate.Core.Errors;
using TicketGate.Core.Metrics;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.WaitingRoom.Commands.LeaveQueue;

/// <summary>
/// Kullaniciyi Redis Sorted Set'ten ZREM ile cikarir.
/// Kullanici kuyruga girmemisse 404 donerek istemciye stale state bilgisini acik eder.
/// </summary>
public sealed class LeaveQueueHandler(
    IConnectionMultiplexer redis,
    IActiveCheckoutService activeCheckoutService)
    : IRequestHandler<LeaveQueueCommand, Result>
{
    /// <summary>
    /// ZREM sonucundaki silinen eleman sayisini kontrol eder.
    /// Sifir sonuc kullanicinin bu event kuyrugunda olmadigini gosterir.
    /// </summary>
    public async ValueTask<Result> Handle(LeaveQueueCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var queueKey = ToWaitingRoomKey(request.EventId);
        var removed = await db.SortedSetRemoveAsync(queueKey, request.UserId.ToString());
        var activeCheckoutRemoved = await activeCheckoutService.DecrementAsync(
            request.EventId,
            request.UserId,
            cancellationToken);

        if (removed)
        {
            var queueDepth = await db.SortedSetLengthAsync(queueKey);
            TicketGateMetrics.WaitingRoomDepth.WithLabels(request.EventId.ToString()).Set(queueDepth);
        }

        return removed || activeCheckoutRemoved
            ? Result.Ok()
            : Result.Fail(AppError.NotFound("QueuePosition", request.UserId));
    }

    /// <summary>Event id icin waiting room Sorted Set anahtarini uretir.</summary>
    private static RedisKey ToWaitingRoomKey(Guid eventId)
    {
        return $"waitingroom:{eventId}";
    }
}
