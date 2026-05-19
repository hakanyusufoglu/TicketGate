using FluentAssertions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketGate.Core.Errors;
using TicketGate.Payment.Domain.Enums;
using TicketGate.Payment.Features.Payments.Commands.InitiatePayment;
using TicketGate.Payment.Infrastructure.Outbox;
using TicketGate.Payment.Infrastructure.Persistence;

namespace TicketGate.Payment.Tests.Features;

/// <summary>
/// InitiatePayment handler integration testleri.
/// Outbox atomikligi ve idempotency davranisi gercek PostgreSQL uzerinde dogrulanir.
/// </summary>
public sealed class InitiatePaymentTests : PaymentIntegrationTestBase
{
    /// <summary>
    /// Gecerli istekte Payment ve OutboxMessage'in ayni transaction'da yazildigini dogrular.
    /// Handler harici gateway'i cagirmadan yalnizca outbox mesaji uretmelidir.
    /// </summary>
    [Fact]
    public async Task Handle_ValidRequest_CreatesPaymentAndOutbox()
    {
        await ResetAsync();
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        SetReservedTicket(ticketId, userId, 725m);
        var command = new InitiatePaymentCommand(ticketId, userId, "Stripe", "payment-key-1");

        var result = await SendScopedAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(PaymentStatus.Pending.ToString());

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = await db.Payments.SingleAsync();
        var outbox = await db.OutboxMessages.SingleAsync();

        payment.Id.Should().Be(result.Value.PaymentId);
        payment.TicketId.Should().Be(ticketId);
        payment.UserId.Should().Be(userId);
        payment.Amount.Should().Be(725m);
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.IdempotencyKey.Should().Be("payment-key-1");

        outbox.Type.Should().Be(OutboxMessageTypes.PaymentInitiated);
        outbox.Payload.Should().Contain(payment.Id.ToString());
        outbox.ProcessedAt.Should().BeNull();
        outbox.RetryCount.Should().Be(0);
    }

    /// <summary>
    /// Command icindeki UserId ile odeme baslatilabildigini dogrular.
    /// Handler HttpContextAccessor bagimliligi olmadan test ve worker ortaminda calisabilmelidir.
    /// </summary>
    [Fact]
    public async Task Handle_CommandUserId_CreatesPaymentAndOutbox()
    {
        await ResetAsync();
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetReservedTicket(ticketId, userId);

        var result = await SendScopedAsync(new InitiatePaymentCommand(
            ticketId,
            userId,
            "Stripe",
            "payment-key-command-user"));

        result.IsSuccess.Should().BeTrue();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = await db.Payments.SingleAsync();
        payment.UserId.Should().Be(userId);
    }

    /// <summary>
    /// Ayni IdempotencyKey ile ikinci istekte mevcut response'un dondugunu dogrular.
    /// Duplicate istek yeni Payment veya OutboxMessage olusturmamalidir.
    /// </summary>
    [Fact]
    public async Task Handle_DuplicateIdempotencyKey_ReturnsSameResponse()
    {
        await ResetAsync();
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetReservedTicket(ticketId, userId);
        var command = new InitiatePaymentCommand(ticketId, userId, "Stripe", "payment-key-2");

        var first = await SendScopedAsync(command);
        var second = await SendScopedAsync(command);

        second.IsSuccess.Should().BeTrue();
        second.Value.Should().NotBeNull();
        second.Value!.PaymentId.Should().Be(first.Value!.PaymentId);
        second.Value.Status.Should().Be(first.Value.Status);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        (await db.Payments.CountAsync()).Should().Be(1);
        (await db.OutboxMessages.CountAsync()).Should().Be(1);
    }

    /// <summary>
    /// Reserved olmayan ticket icin 409 dondugunu dogrular.
    /// Payment modulu Booking tablosuna direkt erismeden Core contract sonucuna gore karar verir.
    /// </summary>
    [Fact]
    public async Task Handle_TicketNotReserved_Returns409()
    {
        await ResetAsync();
        ClearReservedTickets();
        var userId = Guid.NewGuid();

        var result = await SendScopedAsync(new InitiatePaymentCommand(
            Guid.NewGuid(),
            userId,
            "Stripe",
            "payment-key-3"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(AppErrorType.Conflict);
        result.Error.Code.Should().Be("ticket.not_reserved");
    }

    /// <summary>
    /// Farkli kullanicinin ticket'ina odeme baslatmaya calisinca 409 dondugunu dogrular.
    /// UserId eslesmesi payment olusturulmadan once kontrol edilmelidir.
    /// </summary>
    [Fact]
    public async Task Handle_WrongUser_Returns409()
    {
        await ResetAsync();
        var ticketId = Guid.NewGuid();
        SetReservedTicket(ticketId, Guid.NewGuid());
        var userId = Guid.NewGuid();

        var result = await SendScopedAsync(new InitiatePaymentCommand(
            ticketId,
            userId,
            "Stripe",
            "payment-key-4"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(AppErrorType.Conflict);
        result.Error.Code.Should().Be("ticket.lock_owner_mismatch");
    }

    /// <summary>
    /// <summary>
    /// Command'i yeni DI scope icinde gonderir.
    /// Her istek ayri PaymentDbContext kullanarak production request scope davranisini taklit eder.
    /// </summary>
    private async Task<TicketGate.Core.Results.Result<InitiatePaymentResponse>> SendScopedAsync(
        InitiatePaymentCommand command)
    {
        using var scope = Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await sender.Send(command);
    }
}
