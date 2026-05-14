using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TicketGate.Booking.Domain.Entities;
using TicketGate.Booking.Domain.Enums;
using TicketGate.Booking.Features.Tickets.Commands.ReserveTicket;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Core.Errors;

namespace TicketGate.Booking.Tests.Features;

/// <summary>
/// ReserveTicket handler integration testleri.
/// Gercek PostgreSQL xmin concurrency token'i ve Redis SETNX atomik kilidi kullanilir.
/// </summary>
public sealed class ReserveTicketIntegrationTests : BookingIntegrationTestBase
{
    /// <summary>
    /// Musait bir biletin basariyla rezerve edildigini dogrular.
    /// Redis lock alinir, PostgreSQL'e Reserved durumu yazilir.
    /// </summary>
    [Fact]
    public async Task Handle_AvailableTicket_ReservesSuccessfully()
    {
        await ResetAsync();
        var ticket = await CreateTicketAsync();
        var userId = Guid.NewGuid();

        var result = await SendScopedAsync(new ReserveTicketCommand(ticket.Id, userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TicketId.Should().Be(ticket.Id);
        result.Value.Seat.Should().Be(ticket.Seat);
        result.Value.Price.Should().Be(ticket.Price);
        result.Value.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(10), TimeSpan.FromSeconds(5));

        using var verificationScope = Services.CreateScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var reservedTicket = await db.Tickets.SingleAsync(t => t.Id == ticket.Id);
        reservedTicket.Status.Should().Be(TicketStatus.Reserved);
        reservedTicket.LockedByUserId.Should().Be(userId);
        reservedTicket.LockedAt.Should().NotBeNull();

        var redis = Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var lockValue = await redis.StringGetAsync($"ticket:{ticket.Id}:lock");
        lockValue.ToString().Should().Be(userId.ToString());
    }

    /// <summary>
    /// Es zamanli iki istegin yalnizca birini kabul ettigini dogrular.
    /// Redis SETNX atomikligi ayni bilet icin cift rezervasyonu engeller.
    /// </summary>
    [Fact]
    public async Task Handle_ConcurrentRequests_OnlyOneSucceeds()
    {
        await ResetAsync();
        var ticket = await CreateTicketAsync();

        var first = SendScopedAsync(new ReserveTicketCommand(ticket.Id, Guid.NewGuid()));
        var second = SendScopedAsync(new ReserveTicketCommand(ticket.Id, Guid.NewGuid()));

        var results = await Task.WhenAll(first, second);

        results.Count(result => result.IsSuccess).Should().Be(1);
        results.Count(result =>
            result.IsFailure &&
            result.Error?.Type == AppErrorType.Conflict &&
            result.Error.Code == "ticket.already_locked").Should().Be(1);
    }

    /// <summary>
    /// Zaten kilitli bileti rezerve etmeye calisinca 409 conflict dondugunu dogrular.
    /// Handler Postgres'e gitmeden Redis lock sonucuyla istegi reddeder.
    /// </summary>
    [Fact]
    public async Task Handle_AlreadyLocked_Returns409()
    {
        await ResetAsync();
        var ticket = await CreateTicketAsync();
        var redis = Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        await redis.StringSetAsync(
            $"ticket:{ticket.Id}:lock",
            Guid.NewGuid().ToString(),
            TimeSpan.FromMinutes(10),
            When.NotExists);

        var result = await SendScopedAsync(new ReserveTicketCommand(ticket.Id, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(AppErrorType.Conflict);
        result.Error.Code.Should().Be("ticket.already_locked");
    }

    /// <summary>
    /// Olmayan bilet icin 404 dondugunu ve alinan Redis lock'un geri birakildigini dogrular.
    /// Bu hata yolu lock sizintisi uretirse kullanici TTL boyunca gereksiz bloke olur.
    /// </summary>
    [Fact]
    public async Task Handle_TicketNotFound_Returns404()
    {
        await ResetAsync();
        var ticketId = Guid.NewGuid();

        var result = await SendScopedAsync(new ReserveTicketCommand(ticketId, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(AppErrorType.NotFound);

        var redis = Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var exists = await redis.KeyExistsAsync($"ticket:{ticketId}:lock");
        exists.Should().BeFalse();
    }

    /// <summary>
    /// Test bileti olusturur ve PostgreSQL'e kaydeder.
    /// Handler'in gercek EF Core tracking ve xmin davranisiyla calismasi icin seed islemi DbContext uzerinden yapilir.
    /// </summary>
    private async Task<Ticket> CreateTicketAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var ticket = Ticket.Create(Guid.NewGuid(), "A-1", 125.50m);
        await db.Tickets.AddAsync(ticket);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return ticket;
    }

    /// <summary>
    /// Command'i yeni DI scope icinde gonderir.
    /// Es zamanli testlerde her handler'in ayri DbContext almasi production request scope davranisini taklit eder.
    /// </summary>
    private async Task<TicketGate.Core.Results.Result<ReserveTicketResponse>> SendScopedAsync(
        ReserveTicketCommand command)
    {
        using var scope = Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
        return await sender.Send(command);
    }
}
