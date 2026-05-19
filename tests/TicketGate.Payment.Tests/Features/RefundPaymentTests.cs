using FluentAssertions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketGate.Core.Errors;
using TicketGate.Payment.Domain.Enums;
using TicketGate.Payment.Features.Payments.Commands.RefundPayment;
using TicketGate.Payment.Infrastructure.Outbox;
using TicketGate.Payment.Infrastructure.Persistence;
using PaymentEntity = TicketGate.Payment.Domain.Entities.Payment;

namespace TicketGate.Payment.Tests.Features;

/// <summary>
/// RefundPayment handler integration testleri.
/// Iade talebinin sadece Completed ve kullaniciya ait payment icin outbox'a yazildigini dogrular.
/// </summary>
public sealed class RefundPaymentTests : PaymentIntegrationTestBase
{
    /// <summary>
    /// Completed payment icin refund talebi outbox mesaji olusturur.
    /// Handler harici gateway'i cagirmaz; fiili iade OutboxWorker tarafindan tamamlanir.
    /// </summary>
    [Fact]
    public async Task Handle_CompletedPayment_CreatesRefundOutbox()
    {
        await ResetAsync();
        var userId = Guid.NewGuid();
        var paymentId = await CreateCompletedPaymentAsync(userId);

        var result = await SendScopedAsync(new RefundPaymentCommand(paymentId, userId));

        result.IsSuccess.Should().BeTrue();

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = await db.Payments.SingleAsync(payment => payment.Id == paymentId);
        var outbox = await db.OutboxMessages.SingleAsync();

        payment.Status.Should().Be(PaymentStatus.Completed);
        outbox.Type.Should().Be(OutboxMessageTypes.PaymentRefundRequested);
        outbox.Payload.Should().Contain(paymentId.ToString());
        outbox.ProcessedAt.Should().BeNull();
    }

    /// <summary>
    /// Completed olmayan payment icin refund talebi 409 donmelidir.
    /// Pending payment harici gateway sonucunu almadan iade edilebilir kabul edilemez.
    /// </summary>
    [Fact]
    public async Task Handle_PaymentNotCompleted_Returns409()
    {
        await ResetAsync();
        var userId = Guid.NewGuid();
        var paymentId = await CreatePendingPaymentAsync(userId);

        var result = await SendScopedAsync(new RefundPaymentCommand(paymentId, userId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(AppErrorType.Conflict);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        (await db.OutboxMessages.CountAsync()).Should().Be(0);
    }

    /// <summary>
    /// Baska kullanicinin payment kaydi icin refund talebi 401 donmelidir.
    /// UserId client body'den degil JWT akisiyle gelen command alanindan kontrol edilir.
    /// </summary>
    [Fact]
    public async Task Handle_WrongUser_Returns401()
    {
        await ResetAsync();
        var paymentId = await CreateCompletedPaymentAsync(Guid.NewGuid());

        var result = await SendScopedAsync(new RefundPaymentCommand(paymentId, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(AppErrorType.Unauthorized);
    }

    /// <summary>
    /// Test icin Completed payment olusturur.
    /// Complete metodu external payment referansini set ederek refund icin gerekli gateway id'sini hazirlar.
    /// </summary>
    private async Task<Guid> CreateCompletedPaymentAsync(Guid userId)
    {
        var payment = PaymentEntity.Create(Guid.NewGuid(), userId, 500m, Guid.NewGuid().ToString());
        payment.Complete($"external-{payment.Id}");

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await db.Payments.AddAsync(payment);
        await db.SaveChangesAsync();
        return payment.Id;
    }

    /// <summary>
    /// Test icin Pending payment olusturur.
    /// Pending durumdaki payment refund edilemez ve outbox mesaji uretmemelidir.
    /// </summary>
    private async Task<Guid> CreatePendingPaymentAsync(Guid userId)
    {
        var payment = PaymentEntity.Create(Guid.NewGuid(), userId, 500m, Guid.NewGuid().ToString());

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await db.Payments.AddAsync(payment);
        await db.SaveChangesAsync();
        return payment.Id;
    }

    /// <summary>
    /// Command'i yeni DI scope icinde gonderir.
    /// Her test production request scope davranisini taklit eder.
    /// </summary>
    private async Task<TicketGate.Core.Results.Result> SendScopedAsync(RefundPaymentCommand command)
    {
        await using var scope = Services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await sender.Send(command);
    }
}
