using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TicketGate.Booking.Configuration;
using TicketGate.Booking.Domain.Entities;
using TicketGate.Booking.Domain.Enums;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Booking.Infrastructure.Workers;

namespace TicketGate.Booking.Tests.Features;

/// <summary>
/// TicketLockExpiredWorker integration testleri.
/// Gercek Redis keyspace notification davranisi ve PostgreSQL state gecisi birlikte dogrulanir.
/// </summary>
public sealed class TicketLockExpiredWorkerTests : BookingIntegrationTestBase
{
    private static readonly TimeSpan WorkerStartupDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan AssertionTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan AssertionPollInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan TestLockTtl = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ExpiredLockAge = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Redis lock TTL doldugunda ticket'in Available durumuna dondugunu dogrular.
    /// Worker keyspace notification ile tetiklenir ve Postgres kaydi guncellenir.
    /// </summary>
    [Fact]
    public async Task WhenLockExpires_TicketBecomesAvailable()
    {
        await ResetAsync();
        var ticket = await CreateReservedTicketAsync();
        var redis = Services.GetRequiredService<IConnectionMultiplexer>();
        using var worker = CreateWorker(TestLockTtl);
        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(WorkerStartupDelay);

        await redis.GetDatabase().StringSetAsync(
            $"ticket:{ticket.Id}:lock",
            ticket.LockedByUserId!.Value.ToString(),
            TestLockTtl,
            When.NotExists);

        await WaitUntilAsync(async () =>
        {
            var updated = await GetTicketAsync(ticket.Id);
            return updated.Status == TicketStatus.Available && updated.LockedByUserId is null;
        });

        var released = await GetTicketAsync(ticket.Id);
        released.Status.Should().Be(TicketStatus.Available);
        released.LockedByUserId.Should().BeNull();
        released.LockedAt.Should().BeNull();

        await worker.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// Uygulama baslangicinda suresi gecmis Reserved ticket'larin Release() edildigini dogrular.
    /// Keyspace event kacirilmis olsa bile recovery taramasi state'i tutarli hale getirir.
    /// </summary>
    [Fact]
    public async Task OnStartup_ExpiredReservedTickets_AreReleased()
    {
        await ResetAsync();
        var ticket = await CreateReservedTicketAsync(DateTime.UtcNow.Subtract(ExpiredLockAge));
        using var worker = CreateWorker(TestLockTtl);

        await worker.StartAsync(CancellationToken.None);

        await WaitUntilAsync(async () =>
        {
            var updated = await GetTicketAsync(ticket.Id);
            return updated.Status == TicketStatus.Available && updated.LockedAt is null;
        });

        var released = await GetTicketAsync(ticket.Id);
        released.Status.Should().Be(TicketStatus.Available);
        released.LockedByUserId.Should().BeNull();
        released.LockedAt.Should().BeNull();

        await worker.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// Test icin Reserved durumunda ticket olusturur.
    /// LockedAt gecmise cekilmek istenirse EF Core property entry uzerinden ayarlanir.
    /// </summary>
    private async Task<Ticket> CreateReservedTicketAsync(DateTime? lockedAt = null)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var ticket = Ticket.Create(Guid.NewGuid(), "VIP-A-1", 500m);
        ticket.Reserve(Guid.NewGuid());

        await db.Tickets.AddAsync(ticket);
        await db.SaveChangesAsync();

        if (lockedAt is not null)
        {
            db.Entry(ticket).Property(nameof(Ticket.LockedAt)).CurrentValue = lockedAt.Value;
            await db.SaveChangesAsync();
        }

        return ticket;
    }

    /// <summary>
    /// Worker'i test scope'undaki Redis ve DbContext servisleriyle olusturur.
    /// Lock TTL test senaryosuna gore options uzerinden verilir.
    /// </summary>
    private TicketLockExpiredWorker CreateWorker(TimeSpan lockTtl)
    {
        return new TicketLockExpiredWorker(
            Services.GetRequiredService<IConnectionMultiplexer>(),
            Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new BookingSettings { LockTtlSeconds = (int)lockTtl.TotalSeconds }),
            NullLogger<TicketLockExpiredWorker>.Instance);
    }

    /// <summary>
    /// Ticket'i yeni scope ile okur.
    /// Change tracker cache'i test sonucunu etkilemesin diye her okuma izole edilir.
    /// </summary>
    private async Task<Ticket> GetTicketAsync(Guid ticketId)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        return await db.Tickets.SingleAsync(ticket => ticket.Id == ticketId);
    }

    /// <summary>
    /// Asenkron kosul saglanana kadar kisa araliklarla bekler.
    /// Redis expired event'i zamanlamaya bagli oldugu icin sabit uyku yerine polling kullanilir.
    /// </summary>
    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow.Add(AssertionTimeout);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(AssertionPollInterval);
        }

        throw new TimeoutException("Condition was not met before timeout.");
    }
}
