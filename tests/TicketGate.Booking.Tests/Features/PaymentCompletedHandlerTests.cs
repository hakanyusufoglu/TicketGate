using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TicketGate.Booking.Domain.Entities;
using TicketGate.Booking.Domain.Enums;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Core.Events;

namespace TicketGate.Booking.Tests.Features;

/// <summary>
/// PaymentCompleted event handler integration testleri.
/// Odeme basarili tamamlandiginda Reserved ticket'in Confirmed durumuna gectigini dogrular.
/// </summary>
public sealed class PaymentCompletedHandlerTests : BookingIntegrationTestBase
{
    /// <summary>
    /// PaymentCompleted event'i Reserved ticket'i Confirmed yapmali ve Redis lock'u temizlemelidir.
    /// Bu test tam odeme dongusundeki Worker -> Complete -> Confirm kismini dogrular.
    /// </summary>
    [Fact]
    public async Task Handle_ReservedTicket_ConfirmsTicketAndDeletesLock()
    {
        await ResetAsync();
        var userId = Guid.NewGuid();
        var ticketId = await CreateReservedTicketAsync(userId);
        await SetLockAsync(ticketId, userId);

        await PublishScopedAsync(new PaymentCompleted(Guid.NewGuid(), ticketId, userId));

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        var ticket = await db.Tickets.SingleAsync(ticket => ticket.Id == ticketId);

        ticket.Status.Should().Be(TicketStatus.Confirmed);
        ticket.BookedByUserId.Should().Be(userId);
        ticket.LockedByUserId.Should().Be(userId);
        (await redis.GetDatabase().KeyExistsAsync(ToLockKey(ticketId))).Should().BeFalse();
    }

    /// <summary>
    /// Test icin Reserved ticket olusturur.
    /// Domain state machine korunur; alanlar test icin dogrudan degistirilmez.
    /// </summary>
    private async Task<Guid> CreateReservedTicketAsync(Guid userId)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var ticket = Ticket.Create(Guid.NewGuid(), "B-1", 500m);
        ticket.Reserve(userId);

        await db.Tickets.AddAsync(ticket);
        await db.SaveChangesAsync();
        return ticket.Id;
    }

    /// <summary>
    /// Test icin Redis ticket lock anahtarini yazar.
    /// Handler odeme basarisindan sonra TTL beklemeden bu anahtari silmelidir.
    /// </summary>
    private async Task SetLockAsync(Guid ticketId, Guid userId)
    {
        await using var scope = Services.CreateAsyncScope();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        await redis.GetDatabase().StringSetAsync(ToLockKey(ticketId), userId.ToString());
    }

    /// <summary>
    /// Event'i yeni DI scope icinde publish eder.
    /// Production MediatR handler cozumuyle ayni davranisi kullanir.
    /// </summary>
    private async Task PublishScopedAsync(PaymentCompleted notification)
    {
        await using var scope = Services.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.Publish(notification);
    }

    /// <summary>
    /// Ticket id icin Redis lock anahtarini uretir.
    /// Test assertion'i production handler ile ayni key formatini kullanir.
    /// </summary>
    private static RedisKey ToLockKey(Guid ticketId)
    {
        return $"ticket:{ticketId}:lock";
    }
}
