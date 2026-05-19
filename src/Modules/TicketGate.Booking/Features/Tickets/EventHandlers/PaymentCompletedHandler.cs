using Mediator;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TicketGate.Booking.Domain.Enums;
using TicketGate.Core.Events;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Core.Metrics;

namespace TicketGate.Booking.Features.Tickets.EventHandlers;

/// <summary>
/// PaymentCompleted entegrasyon event'ini dinler.
/// Odeme tamamlaninca ticket'i Reserved durumundan Confirmed durumuna gecirir ve Redis lock'u temizler.
/// </summary>
public sealed class PaymentCompletedHandler(
    BookingDbContext db,
    IConnectionMultiplexer redis,
    IMediator mediator) : INotificationHandler<PaymentCompleted>
{
    /// <summary>
    /// Ticket'i Reserved durumundaysa Confirmed yapar.
    /// Redis lock temizlenir ve TicketConfirmed event'i publish edilerek Notification modulu icin akis korunur.
    /// </summary>
    public async ValueTask Handle(PaymentCompleted notification, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(
            item => item.Id == notification.TicketId,
            cancellationToken);

        if (ticket is null || ticket.Status != TicketStatus.Reserved)
        {
            return;
        }

        ticket.Confirm(notification.UserId);
        await db.SaveChangesAsync(cancellationToken);

        if (await redis.GetDatabase().KeyDeleteAsync(ToLockKey(notification.TicketId)))
        {
            TicketGateMetrics.DecrementActiveLocks();
        }

        await mediator.Publish(
            new TicketConfirmed(ticket.Id, ticket.EventId, notification.UserId),
            cancellationToken);
    }

    /// <summary>
    /// Ticket id icin Redis lock anahtarini uretir.
    /// Odeme tamamlaninca TTL beklenmeden lock temizlenmelidir.
    /// </summary>
    private static RedisKey ToLockKey(Guid ticketId)
    {
        return $"ticket:{ticketId}:lock";
    }
}
