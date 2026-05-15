using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketGate.Booking.Domain.Enums;
using TicketGate.Booking.Domain.Events;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Core.Events;

namespace TicketGate.Booking.Features.Tickets.EventHandlers;

/// <summary>
/// PaymentRefunded entegrasyon event'ini dinler.
/// Iade tamamlaninca ticket'i Confirmed durumundan Available durumuna gecirir.
/// Bilet tekrar satisa acilir; moduller arasi iletisim domain event uzerinden korunur.
/// </summary>
public sealed class PaymentRefundedHandler(
    BookingDbContext db,
    IPublisher publisher) : INotificationHandler<PaymentRefunded>
{
    /// <summary>
    /// Ticket Confirmed durumundaysa ReleaseAfterRefund() ile Available yapar.
    /// TicketReleased event'i publish edilerek SSE/notification fan-out akisi tetiklenir.
    /// Ticket bulunamazsa veya Confirmed degilse idempotent sekilde sessizce cikar.
    /// </summary>
    public async Task Handle(PaymentRefunded notification, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(
            item => item.Id == notification.TicketId,
            cancellationToken);

        if (ticket is null || ticket.Status != TicketStatus.Confirmed)
        {
            return;
        }

        ticket.ReleaseAfterRefund();
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new TicketReleased(ticket.Id, ticket.EventId, notification.UserId),
            cancellationToken);
    }
}
