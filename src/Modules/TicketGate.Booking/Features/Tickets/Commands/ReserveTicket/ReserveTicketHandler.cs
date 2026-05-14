using MediatR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TicketGate.Booking.Domain.Enums;
using TicketGate.Booking.Domain.Events;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.Tickets.Commands.ReserveTicket;

/// <summary>
/// Bilet rezervasyon komutunu isler. Redis SETNX ile atomik kilit alir ve TTL=600s uygular.
/// Ayni bilete es zamanli istek gelirse 409 doner. Kilit alindiktan sonra PostgreSQL'e yazar; hata olursa kilidi geri birakir.
/// xmin token ile optimistic concurrency cakismasi da yakalanir.
/// </summary>
internal sealed class ReserveTicketHandler(
    BookingDbContext db,
    IConnectionMultiplexer redis,
    IPublisher publisher) : IRequestHandler<ReserveTicketCommand, Result<ReserveTicketResponse>>
{
    private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Rezervasyon akisini Redis lock, PostgreSQL state kontrolu, domain state gecisi ve event publish adimlariyla yurutur.
    /// Redis SETNX race condition'i engeller; xmin ise ayni satirdaki gec yazma cakismalarini 409'a cevirir.
    /// </summary>
    public async Task<Result<ReserveTicketResponse>> Handle(
        ReserveTicketCommand request,
        CancellationToken cancellationToken)
    {
        var redisDb = redis.GetDatabase();
        var lockKey = ToLockKey(request.TicketId);
        var lockValue = request.UserId.ToString();
        var lockTaken = await redisDb.StringSetAsync(lockKey, lockValue, LockTtl, When.NotExists);

        if (!lockTaken)
        {
            return Result<ReserveTicketResponse>.Fail(AppError.TicketAlreadyLocked(request.TicketId));
        }

        try
        {
            var ticket = await db.Tickets.SingleOrDefaultAsync(
                item => item.Id == request.TicketId,
                cancellationToken);

            if (ticket is null)
            {
                await ReleaseOwnedLockAsync(redisDb, lockKey, lockValue);
                return Result<ReserveTicketResponse>.Fail(AppError.NotFound("Ticket", request.TicketId));
            }

            if (ticket.Status != TicketStatus.Available)
            {
                await ReleaseOwnedLockAsync(redisDb, lockKey, lockValue);
                return Result<ReserveTicketResponse>.Fail(AppError.Conflict(
                    "ticket.not_available",
                    $"Ticket '{request.TicketId}' is not available."));
            }

            ticket.Reserve(request.UserId);
            await db.SaveChangesAsync(cancellationToken);

            var expiresAt = DateTime.UtcNow.Add(LockTtl);
            await publisher.Publish(
                new TicketReserved(ticket.Id, ticket.EventId, ticket.Seat, ticket.Price, request.UserId, expiresAt),
                cancellationToken);

            return Result<ReserveTicketResponse>.Ok(new ReserveTicketResponse(
                ticket.Id,
                ticket.Seat,
                ticket.Price,
                expiresAt));
        }
        catch (DbUpdateConcurrencyException)
        {
            await ReleaseOwnedLockAsync(redisDb, lockKey, lockValue);
            return Result<ReserveTicketResponse>.Fail(AppError.ConcurrencyConflict());
        }
    }

    /// <summary>
    /// Ticket id icin Redis lock anahtarini uretir.
    /// Anahtar formati ticket:{ticketId}:lock standardina sabitlenir.
    /// </summary>
    private static RedisKey ToLockKey(Guid ticketId)
    {
        return $"ticket:{ticketId}:lock";
    }

    /// <summary>
    /// Yalnizca bu handler'in aldigi Redis lock'u siler.
    /// Lua karsilastirma-silme islemi yanlislikla baska kullanicinin lock'unu kaldirma riskini engeller.
    /// </summary>
    private static async Task ReleaseOwnedLockAsync(IDatabase redisDb, RedisKey lockKey, RedisValue lockValue)
    {
        const string script = """
            if redis.call("GET", KEYS[1]) == ARGV[1] then
                return redis.call("DEL", KEYS[1])
            end
            return 0
            """;

        await redisDb.ScriptEvaluateAsync(script, [lockKey], [lockValue]);
    }
}
