using MediatR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TicketGate.Booking.Domain.Enums;
using TicketGate.Booking.Domain.Events;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.Tickets.Commands.ConfirmTicket;

/// <summary>
/// Bileti onaylar. Redis lock sahibi kontrolu yapilir ve yalnizca lock'u elinde tutan kullanici onaylayabilir.
/// Reserved durumundan Confirmed durumuna gecilir, TicketConfirmed event'i yayinlanir.
/// </summary>
internal sealed class ConfirmTicketHandler(
    BookingDbContext db,
    IConnectionMultiplexer redis,
    IPublisher publisher) : IRequestHandler<ConfirmTicketCommand, Result>
{
    /// <summary>
    /// Onay akisini PostgreSQL state kontrolu, Redis lock sahipligi ve xmin korumali kayit adimlariyla yurutur.
    /// Basarili onaydan sonra Redis lock silinir; aksi halde TTL dolana kadar gereksiz blokaj olusur.
    /// </summary>
    public async Task<Result> Handle(ConfirmTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.SingleOrDefaultAsync(
            item => item.Id == request.TicketId,
            cancellationToken);

        if (ticket is null)
        {
            return Result.Fail(AppError.NotFound("Ticket", request.TicketId));
        }

        if (ticket.Status != TicketStatus.Reserved)
        {
            return Result.Fail(AppError.Conflict(
                "ticket.not_reserved",
                $"Ticket '{request.TicketId}' is not reserved."));
        }

        var redisDb = redis.GetDatabase();
        var lockKey = ToLockKey(request.TicketId);
        var lockOwner = await redisDb.StringGetAsync(lockKey);

        if (!lockOwner.HasValue || lockOwner.ToString() != request.UserId.ToString())
        {
            return Result.Fail(AppError.Conflict(
                "ticket.lock_owner_mismatch",
                $"Ticket '{request.TicketId}' is locked by another user."));
        }

        try
        {
            ticket.Confirm(request.UserId);
            await db.SaveChangesAsync(cancellationToken);
            await redisDb.KeyDeleteAsync(lockKey);
            await publisher.Publish(new TicketConfirmed(ticket.Id, ticket.EventId, request.UserId), cancellationToken);
            return Result.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Fail(AppError.ConcurrencyConflict());
        }
    }

    /// <summary>
    /// Ticket id icin Redis lock anahtarini uretir.
    /// Confirm akisi lock sahipligini bu anahtar uzerinden dogrular.
    /// </summary>
    private static RedisKey ToLockKey(Guid ticketId)
    {
        return $"ticket:{ticketId}:lock";
    }
}
