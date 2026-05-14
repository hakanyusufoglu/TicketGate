using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketGate.Booking.Domain.Enums;
using TicketGate.Booking.Domain.Events;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.Tickets.Commands.CancelTicket;

/// <summary>
/// Bileti iptal eder. Confirmed durumundan Cancelled durumuna gecis yapar.
/// TicketCancelled event'i yayinlanir; odeme iadesi Payment modulu tarafindan islenir.
/// </summary>
internal sealed class CancelTicketHandler(
    BookingDbContext db,
    IPublisher publisher) : IRequestHandler<CancelTicketCommand, Result>
{
    /// <summary>
    /// Iptal akisinda biletin varligini, Confirmed durumunu ve bilet sahibi bilgisini kontrol eder.
    /// xmin concurrency token ayni bilete es zamanli iptal veya guncelleme cakismasini 409'a cevirir.
    /// </summary>
    public async Task<Result> Handle(CancelTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.SingleOrDefaultAsync(
            item => item.Id == request.TicketId,
            cancellationToken);

        if (ticket is null)
        {
            return Result.Fail(AppError.NotFound("Ticket", request.TicketId));
        }

        if (ticket.Status != TicketStatus.Confirmed)
        {
            return Result.Fail(AppError.Conflict(
                "ticket.not_confirmed",
                $"Ticket '{request.TicketId}' is not confirmed."));
        }

        if (ticket.BookedByUserId != request.UserId)
        {
            return Result.Fail(AppError.Conflict(
                "ticket.owner_mismatch",
                $"Ticket '{request.TicketId}' belongs to another user."));
        }

        try
        {
            ticket.Cancel();
            await db.SaveChangesAsync(cancellationToken);
            await publisher.Publish(new TicketCancelled(ticket.Id, ticket.EventId, request.UserId), cancellationToken);
            return Result.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Fail(AppError.ConcurrencyConflict());
        }
    }
}
