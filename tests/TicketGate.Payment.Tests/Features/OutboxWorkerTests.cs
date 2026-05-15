using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TicketGate.Payment.Configuration;
using TicketGate.Payment.Domain.Enums;
using TicketGate.Payment.Infrastructure.Outbox;
using TicketGate.Payment.Infrastructure.Persistence;
using TicketGate.Payment.Infrastructure.Workers;
using TicketGate.Payment.Infrastructure.Workers.Payloads;
using PaymentEntity = TicketGate.Payment.Domain.Entities.Payment;

namespace TicketGate.Payment.Tests.Features;

/// <summary>
/// OutboxWorker integration testleri.
/// Gercek PostgreSQL ve kontrollu MockPaymentGateway kullanilir.
/// </summary>
public sealed class OutboxWorkerTests : PaymentIntegrationTestBase
{
    /// <summary>
    /// OutboxWorker calisinca Payment'in Completed'a gectigini ve OutboxMessage'in islendigini dogrular.
    /// Basarili gateway sonucu ExternalPaymentId olarak payment kaydina yazilmalidir.
    /// </summary>
    [Fact]
    public async Task Worker_ProcessesPendingMessage_CompletesPayment()
    {
        await ResetAsync();
        SetGatewayChargeSuccess("external-payment-1");
        var paymentId = await CreatePendingPaymentWithOutboxAsync("worker-key-1");
        var worker = CreateWorker();

        await worker.ProcessPendingMessagesAsync(CancellationToken.None);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = await db.Payments.SingleAsync(payment => payment.Id == paymentId);
        var outbox = await db.OutboxMessages.SingleAsync();

        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.ExternalPaymentId.Should().Be("external-payment-1");
        payment.CompletedAt.Should().NotBeNull();
        outbox.ProcessedAt.Should().NotBeNull();
        outbox.RetryCount.Should().Be(0);
    }

    /// <summary>
    /// Gateway basarisiz olunca RetryCount'un arttigini dogrular.
    /// Mesaj max retry sinirina ulasmadan processed olarak isaretlenmemelidir.
    /// </summary>
    [Fact]
    public async Task Worker_GatewayFails_IncrementsRetryCount()
    {
        await ResetAsync();
        SetGatewayChargeFailure("Gateway timeout");
        var paymentId = await CreatePendingPaymentWithOutboxAsync("worker-key-2");
        var worker = CreateWorker();

        await worker.ProcessPendingMessagesAsync(CancellationToken.None);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = await db.Payments.SingleAsync(payment => payment.Id == paymentId);
        var outbox = await db.OutboxMessages.SingleAsync();

        payment.Status.Should().Be(PaymentStatus.Pending);
        outbox.RetryCount.Should().Be(1);
        outbox.ProcessedAt.Should().BeNull();
        outbox.Error.Should().Contain("Gateway timeout");
    }

    /// <summary>
    /// MaxRetryCount asilinca mesaj dead letter durumuna gecer ve Payment Failed olur.
    /// Bu durumda ticket release akisi PaymentFailed event handler tarafindan ayrica tamamlanir.
    /// </summary>
    [Fact]
    public async Task Worker_MaxRetryExceeded_MarksDeadLetter()
    {
        await ResetAsync();
        SetGatewayChargeFailure("Gateway rejected payment");
        var paymentId = await CreatePendingPaymentWithOutboxAsync("worker-key-3", retryCount: 2);
        var worker = CreateWorker();

        await worker.ProcessPendingMessagesAsync(CancellationToken.None);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = await db.Payments.SingleAsync(payment => payment.Id == paymentId);
        var outbox = await db.OutboxMessages.SingleAsync();

        payment.Status.Should().Be(PaymentStatus.Failed);
        outbox.RetryCount.Should().Be(3);
        outbox.ProcessedAt.Should().BeNull();
        outbox.IsDeadLetter(new OutboxSettings().MaxRetryCount).Should().BeTrue();
    }

    /// <summary>
    /// Refund outbox mesaji islenince Payment'in Refunded'a gectigini ve mesajin processed oldugunu dogrular.
    /// PaymentRefunded event'i ayni akis icinde publish edilir; Booking state gecisi kendi handler'inda test edilir.
    /// </summary>
    [Fact]
    public async Task Worker_ProcessesRefundMessage_RefundsPayment()
    {
        await ResetAsync();
        var paymentId = await CreateCompletedPaymentWithRefundOutboxAsync("refund-key-1");
        var worker = CreateWorker();

        await worker.ProcessPendingMessagesAsync(CancellationToken.None);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = await db.Payments.SingleAsync(payment => payment.Id == paymentId);
        var outbox = await db.OutboxMessages.SingleAsync();

        payment.Status.Should().Be(PaymentStatus.Refunded);
        outbox.Type.Should().Be(OutboxMessageTypes.PaymentRefundRequested);
        outbox.ProcessedAt.Should().NotBeNull();
        outbox.RetryCount.Should().Be(0);
    }

    /// <summary>
    /// Pending payment ve PaymentInitiated outbox mesajini ayni test veritabanina yazar.
    /// RetryCount senaryosu gerekiyorsa MarkFailed ile mevcut retry sayisi hazirlanir.
    /// </summary>
    private async Task<Guid> CreatePendingPaymentWithOutboxAsync(string idempotencyKey, int retryCount = 0)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = PaymentEntity.Create(Guid.NewGuid(), Guid.NewGuid(), 500m, idempotencyKey);
        var outbox = OutboxMessage.Create(
            OutboxMessageTypes.PaymentInitiated,
            new PaymentInitiatedOutboxPayload(
                payment.Id,
                payment.TicketId,
                payment.UserId,
                payment.Amount,
                payment.Currency,
                "Stripe"));

        for (var i = 0; i < retryCount; i++)
        {
            outbox.MarkFailed("previous failure");
        }

        await db.Payments.AddAsync(payment);
        await db.OutboxMessages.AddAsync(outbox);
        await db.SaveChangesAsync();
        return payment.Id;
    }

    /// <summary>
    /// Completed payment ve refund outbox mesajini ayni test veritabanina yazar.
    /// Refund akisi pending charge'tan ayri olarak gateway refund sonucuna gore tamamlanir.
    /// </summary>
    private async Task<Guid> CreateCompletedPaymentWithRefundOutboxAsync(string idempotencyKey)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var payment = PaymentEntity.Create(Guid.NewGuid(), Guid.NewGuid(), 500m, idempotencyKey);
        payment.Complete($"external-{payment.Id}");
        var outbox = OutboxMessage.Create(
            OutboxMessageTypes.PaymentRefundRequested,
            new RefundPaymentOutboxPayload(
                payment.Id,
                payment.TicketId,
                payment.UserId,
                payment.ExternalPaymentId!));

        await db.Payments.AddAsync(payment);
        await db.OutboxMessages.AddAsync(outbox);
        await db.SaveChangesAsync();
        return payment.Id;
    }

    /// <summary>
    /// Test servisleriyle OutboxWorker olusturur.
    /// Worker dongusu yerine tek batch isleyen public metod cagrilir.
    /// </summary>
    private OutboxWorker CreateWorker()
    {
        return new OutboxWorker(
            Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxSettings()),
            NullLogger<OutboxWorker>.Instance);
    }
}
