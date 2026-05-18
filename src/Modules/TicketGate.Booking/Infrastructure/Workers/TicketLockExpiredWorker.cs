using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TicketGate.Booking.Configuration;
using TicketGate.Booking.Domain.Enums;
using TicketGate.Core.Events;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Core.Metrics;

namespace TicketGate.Booking.Infrastructure.Workers;

/// <summary>
/// Redis keyspace notification dinleyicisi. ticket:{id}:lock anahtari TTL expire oldugunda tetiklenir.
/// Postgres'teki ticket durumunu Available'a ceker; keyspace notification hizli ama at-most-once oldugu icin startup recovery taramasi da yapar.
/// notify-keyspace-events = KEx Redis config'inde aktif olmalidir.
/// </summary>
public sealed class TicketLockExpiredWorker(
    IConnectionMultiplexer redis,
    IServiceScopeFactory scopeFactory,
    IOptions<BookingSettings> settings,
    ILogger<TicketLockExpiredWorker> logger) : BackgroundService
{
    private const string ExpiredEventChannel = "__keyevent@0__:expired";
    private const string TicketLockPrefix = "ticket:";
    private const string TicketLockSuffix = ":lock";

    /// <summary>
    /// Baslangicta crash recovery taramasi yapar, ardindan Redis expired event'lerini dinler.
    /// Yalnizca ticket:{ticketId}:lock formatindaki key'ler islenir.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverExpiredLocksAsync(stoppingToken);

        var subscriber = redis.GetSubscriber();
        await subscriber.SubscribeAsync(
            new RedisChannel(ExpiredEventChannel, RedisChannel.PatternMode.Literal),
            (channel, expiredKey) =>
            {
                _ = HandleExpiredKeyAsync(expiredKey.ToString(), stoppingToken);
            });

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    /// <summary>
    /// Redis expired key bilgisini ticket lock formatina gore parse eder.
    /// Format uymazsa islem yapmaz; parse hatalari worker dongusunu etkilemez.
    /// </summary>
    private async Task HandleExpiredKeyAsync(string key, CancellationToken cancellationToken)
    {
        if (!TryParseTicketLockKey(key, out var ticketId))
        {
            return;
        }

        logger.LogInformation("Ticket lock expired: {TicketId}", ticketId);
        await ReleaseTicketAsync(ticketId, cancellationToken);
    }

    /// <summary>
    /// Crash recovery icin LockTtlSeconds suresinden daha eski Reserved ticket'lari Release() eder.
    /// Keyspace event kacirilmissa sistem bu tarama ile tutarli duruma geri doner.
    /// </summary>
    private async Task RecoverExpiredLocksAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

        var expiredThreshold = DateTime.UtcNow.AddSeconds(-settings.Value.LockTtlSeconds);
        var expiredTickets = await db.Tickets
            .Where(ticket =>
                ticket.Status == TicketStatus.Reserved &&
                ticket.LockedAt < expiredThreshold)
            .ToListAsync(cancellationToken);

        if (expiredTickets.Count == 0)
        {
            return;
        }

        logger.LogInformation(
            "Crash recovery: {Count} expired lock(s) found",
            expiredTickets.Count);

        foreach (var ticket in expiredTickets)
        {
            ticket.Release();
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Crash recovery: {Count} ticket(s) released",
            expiredTickets.Count);
    }

    /// <summary>
    /// Ticket'i Release() eder, Postgres'e yazar ve TicketReleased domain event'i yayinlar.
    /// Exception durumunda hata loglanir; worker process'i cokertmeden dinlemeye devam eder.
    /// </summary>
    private async Task ReleaseTicketAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            var ticket = await db.Tickets
                .FirstOrDefaultAsync(item => item.Id == ticketId, cancellationToken);

            if (ticket is null || ticket.Status != TicketStatus.Reserved)
            {
                return;
            }

            var lockedByUserId = ticket.LockedByUserId;
            ticket.Release();
            await db.SaveChangesAsync(cancellationToken);
            TicketGateMetrics.ActiveLocks.Dec();

            await publisher.Publish(
                new TicketReleased(ticket.Id, ticket.EventId, lockedByUserId),
                cancellationToken);

            logger.LogInformation(
                "Ticket released: {TicketId} Seat: {Seat}",
                ticket.Id,
                ticket.Seat);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to release ticket: {TicketId}", ticketId);
        }
    }

    /// <summary>
    /// Redis key'ini ticket:{ticketId}:lock formatina gore cozumler.
    /// Gecersiz format veya Guid parse hatasinda false doner.
    /// </summary>
    private static bool TryParseTicketLockKey(string key, out Guid ticketId)
    {
        ticketId = Guid.Empty;

        if (!key.StartsWith(TicketLockPrefix, StringComparison.Ordinal) ||
            !key.EndsWith(TicketLockSuffix, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = key.Split(':');
        return parts.Length == 3 && Guid.TryParse(parts[1], out ticketId);
    }
}
