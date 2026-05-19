using Mediator;
using StackExchange.Redis;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.WaitingRoom.Queries.GetQueuePosition;

/// <summary>
/// Kullanicinin waiting room'daki pozisyonunu doner.
/// Redis ZRANK ile O(log N) sorgu yapar; kullanici kuyrukta degilse 404 doner.
/// </summary>
public sealed class GetQueuePositionHandler(IConnectionMultiplexer redis)
    : IRequestHandler<GetQueuePositionQuery, Result<QueuePositionDto>>
{
    /// <summary>
    /// ZRANK ile pozisyon, ZCARD ile toplam kuyruk buyuklugu alinir.
    /// ZRANK 0-indexed doner; kullaniciya +1 ile gosterilir.
    /// </summary>
    public async ValueTask<Result<QueuePositionDto>> Handle(
        GetQueuePositionQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var queueKey = ToWaitingRoomKey(request.EventId);
        var rank = await db.SortedSetRankAsync(queueKey, request.UserId.ToString(), Order.Ascending);

        if (rank is null)
        {
            return Result<QueuePositionDto>.Fail(AppError.NotFound("QueuePosition", request.UserId));
        }

        var total = await db.SortedSetLengthAsync(queueKey);

        return Result<QueuePositionDto>.Ok(new QueuePositionDto(
            request.EventId,
            request.UserId,
            rank.Value + 1,
            total));
    }

    /// <summary>Event id icin waiting room Sorted Set anahtarini uretir.</summary>
    private static RedisKey ToWaitingRoomKey(Guid eventId)
    {
        return $"waitingroom:{eventId}";
    }
}
