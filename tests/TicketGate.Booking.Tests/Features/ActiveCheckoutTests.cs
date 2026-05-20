using FluentAssertions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TicketGate.Booking.Configuration;
using TicketGate.Booking.Domain.Entities;
using TicketGate.Booking.Domain.Enums;
using TicketGate.Booking.Features.Tickets.Commands.ReserveTicket;
using TicketGate.Booking.Features.WaitingRoom.Commands.JoinQueue;
using TicketGate.Booking.Features.WaitingRoom.Commands.LeaveQueue;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Booking.Infrastructure.Services;
using TicketGate.Booking.Infrastructure.Workers;
using TicketGate.Core.Events;

namespace TicketGate.Booking.Tests.Features;

/// <summary>
/// Active checkout sayaci sizinti testleri.
/// Redis sayaci ve kullanici sahipligi birlikte dogrulanarak her cikis senaryosunda kapasitenin geri verildigi kanitlanir.
/// </summary>
public sealed class ActiveCheckoutTests : BookingIntegrationTestBase
{
    private static readonly TimeSpan WorkerStartupDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan AssertionTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan AssertionPollInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Ayni kullaniciya iki kez checkout hakki verilirse sayacin bir kez arttigini dogrular.
    /// Kullanici sahipligi tutulmazsa retry veya duplicate grant kapasite sizintisi uretir.
    /// </summary>
    [Fact]
    public async Task IncrementAsync_SameUser_IsIdempotent()
    {
        await ResetAsync();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = Services.GetRequiredService<IActiveCheckoutService>();

        await service.IncrementAsync(eventId, userId);
        await service.IncrementAsync(eventId, userId);

        var count = await service.GetCountAsync(eventId);
        count.Should().Be(1);
    }

    /// <summary>
    /// Kuyrukta bekleyen kullanici cikarsa active checkout sayacinin dusmedigini dogrular.
    /// Sadece toplam sayaca bakarak DECR yapmak baska kullanicinin aktif slotunu bozar.
    /// </summary>
    [Fact]
    public async Task LeaveQueue_WaitingUser_DoesNotDecrementActiveCheckout()
    {
        await ResetAsync();
        var eventId = Guid.NewGuid();
        var activeUserId = Guid.NewGuid();
        var waitingUserId = Guid.NewGuid();
        var activeCheckout = Services.GetRequiredService<IActiveCheckoutService>();
        var redis = Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();

        await activeCheckout.IncrementAsync(eventId, activeUserId);
        await redis.SortedSetAddAsync($"waitingroom:{eventId}", waitingUserId.ToString(), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        var result = await SendScopedAsync(new LeaveQueueCommand(eventId, waitingUserId));

        result.IsSuccess.Should().BeTrue();
        (await activeCheckout.GetCountAsync(eventId)).Should().Be(1);
    }

    /// <summary>
    /// Reserve basarisiz olunca kullanicinin checkout slotunun geri verildigini dogrular.
    /// Ticket status uygun degilse active checkout kalici olarak dolu kalmamalidir.
    /// </summary>
    [Fact]
    public async Task Reserve_Fails_DecrementsActiveCheckout()
    {
        await ResetAsync();
        var userId = Guid.NewGuid();
        var ticket = await CreateReservedTicketAsync(Guid.NewGuid());
        var activeCheckout = Services.GetRequiredService<IActiveCheckoutService>();
        await activeCheckout.IncrementAsync(ticket.EventId, userId);

        var result = await SendScopedAsync(new ReserveTicketCommand(ticket.Id, userId));

        result.IsFailure.Should().BeTrue();
        (await activeCheckout.GetCountAsync(ticket.EventId)).Should().Be(0);
    }

    /// <summary>
    /// Ticket lock TTL expire olunca active checkout sayacinin dustugunu dogrular.
    /// Keyspace notification akisi state cleanup ile birlikte waiting room kapasitesini de geri vermelidir.
    /// </summary>
    [Fact]
    public async Task LockExpires_DecrementsActiveCheckout()
    {
        await ResetAsync();
        var userId = Guid.NewGuid();
        var ticket = await CreateReservedTicketAsync(userId);
        var activeCheckout = Services.GetRequiredService<IActiveCheckoutService>();
        await activeCheckout.IncrementAsync(ticket.EventId, userId);
        var redis = Services.GetRequiredService<IConnectionMultiplexer>();
        using var worker = CreateWorker(TimeSpan.FromSeconds(1));
        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(WorkerStartupDelay);

        await redis.GetDatabase().StringSetAsync(
            $"ticket:{ticket.Id}:lock",
            userId.ToString(),
            TimeSpan.FromSeconds(1),
            When.NotExists);

        await WaitUntilAsync(async () => await activeCheckout.GetCountAsync(ticket.EventId) == 0);

        await worker.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// Odeme tamamlaninca active checkout sayacinin dustugunu dogrular.
    /// Confirmed bilet artik checkout kapasitesini isgal etmemelidir.
    /// </summary>
    [Fact]
    public async Task PaymentCompleted_DecrementsActiveCheckout()
    {
        await ResetAsync();
        var userId = Guid.NewGuid();
        var ticket = await CreateReservedTicketAsync(userId);
        var activeCheckout = Services.GetRequiredService<IActiveCheckoutService>();
        await activeCheckout.IncrementAsync(ticket.EventId, userId);

        await PublishScopedAsync(new PaymentCompleted(Guid.NewGuid(), ticket.Id, userId));

        (await activeCheckout.GetCountAsync(ticket.EventId)).Should().Be(0);
    }

    /// <summary>
    /// Iade event'i tekrar veya gec geldiyse active checkout sayacinin negatife dusmedigini dogrular.
    /// Kullanici sahipligi silinmis oldugunda DECR no-op olmalidir.
    /// </summary>
    [Fact]
    public async Task PaymentRefunded_AfterCheckoutClosed_DoesNotGoNegative()
    {
        await ResetAsync();
        var userId = Guid.NewGuid();
        var ticket = await CreateConfirmedTicketAsync(userId);
        var activeCheckout = Services.GetRequiredService<IActiveCheckoutService>();

        await PublishScopedAsync(new PaymentRefunded(Guid.NewGuid(), ticket.Id, userId));

        (await activeCheckout.GetCountAsync(ticket.EventId)).Should().Be(0);
    }

    /// <summary>
    /// Tekrarlanan grant ve cikis dongulerinde sayacin sifira dondugunu dogrular.
    /// Idempotent sahiplik modeli yoksa uzun calismada active_checkout sonsuz buyur.
    /// </summary>
    [Fact]
    public async Task LongRunning_NoLeakInActiveCheckout()
    {
        await ResetAsync();
        var eventId = Guid.NewGuid();
        var activeCheckout = Services.GetRequiredService<IActiveCheckoutService>();

        for (var index = 0; index < 25; index++)
        {
            var userId = Guid.NewGuid();
            await activeCheckout.IncrementAsync(eventId, userId);
            await activeCheckout.IncrementAsync(eventId, userId);
            await activeCheckout.DecrementAsync(eventId, userId);
            await activeCheckout.DecrementAsync(eventId, userId);
        }

        (await activeCheckout.GetCountAsync(eventId)).Should().Be(0);
    }

    /// <summary>
    /// Test bileti olusturur ancak kaydetmez.
    /// Senaryolar state gecisini ayarladiktan sonra SaveTicketAsync ile persist eder.
    /// </summary>
    private static Ticket CreateTicket()
    {
        return Ticket.Create(Guid.NewGuid(), $"A-{Guid.NewGuid():N}", 100m);
    }

    /// <summary>
    /// Available test bileti olusturur ve veritabanina kaydeder.
    /// Handler testlerinde gercek EF Core state'i kullanilir.
    /// </summary>
    private async Task<Ticket> CreateTicketAsync()
    {
        var ticket = CreateTicket();
        await SaveTicketAsync(ticket);
        return ticket;
    }

    /// <summary>
    /// Reserved durumunda test bileti olusturur.
    /// Lock sahibi active checkout cikis senaryolarinda kullanilir.
    /// </summary>
    private async Task<Ticket> CreateReservedTicketAsync(Guid userId)
    {
        var ticket = CreateTicket();
        ticket.Reserve(userId);
        await SaveTicketAsync(ticket);
        return ticket;
    }

    /// <summary>
    /// Confirmed durumunda test bileti olusturur.
    /// Refund handler'i Confirmed -> Available gecisini bu kayit uzerinden test eder.
    /// </summary>
    private async Task<Ticket> CreateConfirmedTicketAsync(Guid userId)
    {
        var ticket = CreateTicket();
        ticket.Reserve(userId);
        ticket.Confirm(userId);
        await SaveTicketAsync(ticket);
        return ticket;
    }

    /// <summary>
    /// Ticket entity'sini yeni scope ile veritabanina yazar.
    /// Testler DbContext tracking etkisinden bagimsiz kalir.
    /// </summary>
    private async Task SaveTicketAsync(Ticket ticket)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        await db.Tickets.AddAsync(ticket);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Command'i yeni DI scope icinde Mediator ile gonderir.
    /// Production request scope davranisina yakin test izolasyonu saglar.
    /// </summary>
    private async Task<TicketGate.Core.Results.Result> SendScopedAsync(
        IRequest<TicketGate.Core.Results.Result> request)
    {
        using var scope = Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await sender.Send(request);
    }

    /// <summary>
    /// ReserveTicket command'ini yeni DI scope icinde Mediator ile gonderir.
    /// Handler kendi scoped BookingDbContext ornegini kullanir.
    /// </summary>
    private async Task<TicketGate.Core.Results.Result<ReserveTicketResponse>> SendScopedAsync(
        ReserveTicketCommand command)
    {
        using var scope = Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await sender.Send(command);
    }

    /// <summary>
    /// Integration event'i yeni DI scope icinde publish eder.
    /// Booking event handler'lari gercek Mediator pipeline'i uzerinden calisir.
    /// </summary>
    private async Task PublishScopedAsync<TNotification>(TNotification notification)
        where TNotification : INotification
    {
        await using var scope = Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Publish(notification);
    }

    /// <summary>
    /// TicketLockExpiredWorker test instance'i olusturur.
    /// TTL ayari test senaryosuna gore options uzerinden verilir.
    /// </summary>
    private TicketLockExpiredWorker CreateWorker(TimeSpan lockTtl)
    {
        return new TicketLockExpiredWorker(
            Services.GetRequiredService<IConnectionMultiplexer>(),
            Services.GetRequiredService<IServiceScopeFactory>(),
            Services.GetRequiredService<IActiveCheckoutService>(),
            Options.Create(new BookingSettings { LockTtlSeconds = (int)lockTtl.TotalSeconds }),
            NullLogger<TicketLockExpiredWorker>.Instance);
    }

    /// <summary>
    /// Asenkron kosul saglanana kadar kisa araliklarla bekler.
    /// Redis keyspace notification zamanlamasi deterministik olmadigi icin polling kullanilir.
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
