using FluentAssertions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketGate.Booking.Domain.Entities;
using TicketGate.Booking.Domain.Enums;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Core.Events;

namespace TicketGate.Booking.Tests.Features;

/// <summary>
/// PaymentRefunded event handler integration testleri.
/// Iade tamamlandiginda Confirmed ticket'in tekrar Available'a dondugunu dogrular.
/// </summary>
public sealed class PaymentRefundedHandlerTests : BookingIntegrationTestBase
{
    /// <summary>
    /// PaymentRefunded event'i yayinlandiginda ticket Confirmed durumundan Available durumuna donmelidir.
    /// Bu akis CancelTicket'tan farklidir; bilet tekrar satisa acilir.
    /// </summary>
    [Fact]
    public async Task Handle_ConfirmedTicket_ReleasesTicketAfterRefund()
    {
        await ResetAsync();
        var userId = Guid.NewGuid();
        var ticketId = await CreateConfirmedTicketAsync(userId);

        await PublishScopedAsync(new PaymentRefunded(Guid.NewGuid(), ticketId, userId));

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var ticket = await db.Tickets.SingleAsync(ticket => ticket.Id == ticketId);

        ticket.Status.Should().Be(TicketStatus.Available);
        ticket.BookedByUserId.Should().BeNull();
        ticket.LockedByUserId.Should().BeNull();
        ticket.LockedAt.Should().BeNull();
    }

    /// <summary>
    /// PaymentRefunded event'i Confirmed olmayan ticket icin state degistirmemelidir.
    /// Reserved ticket ancak TTL expire veya failed payment akisinda Release() edilir.
    /// </summary>
    [Fact]
    public async Task Handle_ReservedTicket_DoesNotReleaseTicket()
    {
        await ResetAsync();
        var userId = Guid.NewGuid();
        var ticketId = await CreateReservedTicketAsync(userId);

        await PublishScopedAsync(new PaymentRefunded(Guid.NewGuid(), ticketId, userId));

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var ticket = await db.Tickets.SingleAsync(ticket => ticket.Id == ticketId);

        ticket.Status.Should().Be(TicketStatus.Reserved);
        ticket.LockedByUserId.Should().Be(userId);
    }

    /// <summary>
    /// Test icin Confirmed ticket olusturur.
    /// Reserve ve Confirm domain metodlari kullanilarak state machine disina cikilmaz.
    /// </summary>
    private async Task<Guid> CreateConfirmedTicketAsync(Guid userId)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var ticket = Ticket.Create(Guid.NewGuid(), "A-1", 500m);
        ticket.Reserve(userId);
        ticket.Confirm(userId);

        await db.Tickets.AddAsync(ticket);
        await db.SaveChangesAsync();
        return ticket.Id;
    }

    /// <summary>
    /// Test icin Reserved ticket olusturur.
    /// Iade event'i bu state icin gecersiz oldugundan handler sessizce cikmalidir.
    /// </summary>
    private async Task<Guid> CreateReservedTicketAsync(Guid userId)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var ticket = Ticket.Create(Guid.NewGuid(), "A-2", 500m);
        ticket.Reserve(userId);

        await db.Tickets.AddAsync(ticket);
        await db.SaveChangesAsync();
        return ticket.Id;
    }

    /// <summary>
    /// Event'i yeni DI scope icinde publish eder.
    /// Production Mediator handler cozumuyle ayni davranisi kullanir.
    /// </summary>
    private async Task PublishScopedAsync(PaymentRefunded notification)
    {
        await using var scope = Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Publish(notification);
    }
}
