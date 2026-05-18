using MediatR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TicketGate.Booking.Domain.Enums;
using TicketGate.Core.Events;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Core.Metrics;

namespace TicketGate.Booking.Features.Tickets.EventHandlers;

/// <summary>
/// PaymentFailed entegrasyon event'ini dinler.
/// Odeme dead letter durumuna dusunce ticket'i tekrar Available durumuna ceker ve Redis lock'u temizler.
/// </summary>
public sealed class PaymentFailedHandler(
    BookingDbContext db,
    IConnectionMultiplexer redis,
    IPublisher publisher) : INotificationHandler<PaymentFailed>
{
    /// <summary>
    /// Ticket'i Reserved durumundaysa Available durumuna alir.
    /// Redis lock manuel silinir ve TicketReleased event'i publish edilerek seat status fan-out akisi korunur.
    /// </summary>
    public async Task Handle(PaymentFailed notification, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(
            item => item.Id == notification.TicketId,
            cancellationToken);

        if (ticket is null || ticket.Status != TicketStatus.Reserved)
        {
            return;
        }

        var releasedUserId = ticket.LockedByUserId;
        ticket.Release();
        await db.SaveChangesAsync(cancellationToken);

        if (await redis.GetDatabase().KeyDeleteAsync(ToLockKey(notification.TicketId)))
        {
            TicketGateMetrics.DecrementActiveLocks();
        }

        await publisher.Publish(
            new TicketReleased(ticket.Id, ticket.EventId, releasedUserId),
            cancellationToken);
    }

    /// <summary>
    /// Ticket id icin Redis lock anahtarini uretir.
    /// Basarisiz odeme sonrasi kullanici TTL boyunca gereksiz bloke edilmemelidir.
    /// </summary>
    private static RedisKey ToLockKey(Guid ticketId)
    {
        return $"ticket:{ticketId}:lock";
    }
}
