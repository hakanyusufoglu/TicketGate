using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TicketGate.Booking.Configuration;
using TicketGate.Booking.Domain.Enums;
using TicketGate.Core.Events;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Booking.Infrastructure.Services;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using TicketGate.Core.Metrics;

namespace TicketGate.Booking.Features.Tickets.Commands.ReserveTicket;

/// <summary>
/// Bilet rezervasyon komutunu isler. Redis SETNX ile atomik kilit alir ve TTL'i BookingSettings uzerinden uygular.
/// Ayni bilete es zamanli istek gelirse 409 doner.
/// finally blogu ile basarisiz tum hata yollarinda owned lock temizlenir; ghost lock problemi engellenir.
/// </summary>
public sealed class ReserveTicketHandler(
    BookingDbContext db,
    IConnectionMultiplexer redis,
    IMediator mediator,
    IActiveCheckoutService activeCheckoutService,
    IOptions<BookingSettings> settings,
    ILogger<ReserveTicketHandler> logger) : IRequestHandler<ReserveTicketCommand, Result<ReserveTicketResponse>>
{
    /// <summary>
    /// Rezervasyon akisini Redis lock, PostgreSQL state kontrolu, domain state gecisi ve event publish adimlariyla yurutur.
    /// Basarisiz olursa finally blogunda lock geri birakilir. Beklenmedik exception tipleri yakalanarak ghost lock engellenir.
    /// </summary>
    public async ValueTask<Result<ReserveTicketResponse>> Handle(
        ReserveTicketCommand request,
        CancellationToken cancellationToken)
    {
        var redisDb = redis.GetDatabase();
        var lockKey = ToLockKey(request.TicketId);
        var lockValue = request.UserId.ToString();
        var lockTtl = TimeSpan.FromSeconds(settings.Value.LockTtlSeconds);
        var success = false;
        Guid? checkoutEventId = null;
        var lockTaken = await redisDb.StringSetAsync(lockKey, lockValue, lockTtl, When.NotExists);

        if (!lockTaken)
        {
            checkoutEventId = await db.Tickets
                .Where(item => item.Id == request.TicketId)
                .Select(item => (Guid?)item.EventId)
                .SingleOrDefaultAsync(cancellationToken);

            if (checkoutEventId is not null)
            {
                await activeCheckoutService.DecrementAsync(checkoutEventId.Value, request.UserId, cancellationToken);
            }

            TicketGateMetrics.TicketReservations.WithLabels("conflict").Inc();
            return Result<ReserveTicketResponse>.Fail(AppError.TicketAlreadyLocked(request.TicketId));
        }

        try
        {
            var ticket = await db.Tickets.SingleOrDefaultAsync(
                item => item.Id == request.TicketId,
                cancellationToken);

            if (ticket is null)
            {
                TicketGateMetrics.TicketReservations.WithLabels("not_found").Inc();
                return Result<ReserveTicketResponse>.Fail(AppError.NotFound("Ticket", request.TicketId));
            }

            checkoutEventId = ticket.EventId;

            if (ticket.Status != TicketStatus.Available)
            {
                TicketGateMetrics.TicketReservations.WithLabels("conflict").Inc();
                return Result<ReserveTicketResponse>.Fail(AppError.Conflict(
                    "ticket.not_available",
                    $"Ticket '{request.TicketId}' is not available."));
            }

            ticket.Reserve(request.UserId);
            await db.SaveChangesAsync(cancellationToken);

            var expiresAt = DateTime.UtcNow.Add(lockTtl);
            await mediator.Publish(
                new TicketReserved(ticket.Id, ticket.EventId, ticket.Seat, ticket.Price, request.UserId, expiresAt),
                cancellationToken);

            TicketGateMetrics.TicketReservations.WithLabels("success").Inc();
            TicketGateMetrics.IncrementActiveLocks();
            success = true;
            await activeCheckoutService.DecrementAsync(ticket.EventId, request.UserId, cancellationToken);

            return Result<ReserveTicketResponse>.Ok(new ReserveTicketResponse(
                ticket.Id,
                ticket.Seat,
                ticket.Price,
                expiresAt));
        }
        catch (DbUpdateConcurrencyException)
        {
            TicketGateMetrics.TicketReservations.WithLabels("conflict").Inc();
            return Result<ReserveTicketResponse>.Fail(AppError.ConcurrencyConflict());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rezervasyon hatasi: {TicketId}", request.TicketId);
            TicketGateMetrics.TicketReservations.WithLabels("error").Inc();

            return Result<ReserveTicketResponse>.Fail(new AppError(
                AppErrorType.Internal,
                "ticket.reservation_failed",
                "Rezervasyon sirasinda beklenmedik bir hata olustu."));
        }
        finally
        {
            if (!success)
            {
                if (checkoutEventId is not null)
                {
                    await activeCheckoutService.DecrementAsync(checkoutEventId.Value, request.UserId, cancellationToken);
                }

                await ReleaseOwnedLockAsync(redisDb, lockKey, lockValue);
                logger.LogDebug("Lock geri birakildi: {TicketId}", request.TicketId);
            }
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
