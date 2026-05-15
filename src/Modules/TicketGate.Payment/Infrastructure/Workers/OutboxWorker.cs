using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketGate.Core.Events;
using TicketGate.Payment.Configuration;
using TicketGate.Payment.Infrastructure.Gateways;
using TicketGate.Payment.Infrastructure.Outbox;
using TicketGate.Payment.Infrastructure.Persistence;
using TicketGate.Payment.Infrastructure.Workers.Payloads;

namespace TicketGate.Payment.Infrastructure.Workers;

/// <summary>
/// Outbox mesajlarini isleyen arka plan servisidir.
/// OutboxSettings aralik ve batch degerleriyle calisir, harici payment gateway cagrilarini handler disinda yurutur.
/// Basarili mesajlar processed olur; retry limiti asilan charge veya refund mesajlari dead letter olarak loglanir.
/// </summary>
public sealed class OutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxSettings> settings,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    private readonly OutboxSettings _settings = settings.Value;

    /// <summary>
    /// PollingIntervalSeconds aralikla outbox tablosunu kontrol eder.
    /// Her turda BatchSize kadar islenmemis mesaj alir ve hatalari worker dongusunu durdurmadan loglar.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OutboxWorker error");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), stoppingToken);
        }
    }

    /// <summary>
    /// Islenmemis outbox mesajlarini batch olarak alir ve isler.
    /// processed_at IS NULL ve retry_count MaxRetryCount altinda olan mesajlar secilir.
    /// </summary>
    public async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var gateway = scope.ServiceProvider.GetRequiredService<IPaymentGateway>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await db.OutboxMessages
            .Where(message => message.ProcessedAt == null && message.RetryCount < _settings.MaxRetryCount)
            .OrderBy(message => message.CreatedAt)
            .Take(_settings.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, db, gateway, publisher, cancellationToken);
        }

        if (messages.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Tek bir outbox mesajini tipine gore gateway'e iletir.
    /// PaymentInitiated charge gateway'e, PaymentRefundRequested refund gateway'e yonlendirilir.
    /// </summary>
    private async Task ProcessMessageAsync(
        OutboxMessage message,
        PaymentDbContext db,
        IPaymentGateway gateway,
        IPublisher publisher,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (message.Type)
            {
                case OutboxMessageTypes.PaymentInitiated:
                    await ProcessPaymentInitiatedAsync(message, db, gateway, publisher, cancellationToken);
                    break;
                case OutboxMessageTypes.PaymentRefundRequested:
                    await ProcessRefundRequestedAsync(message, db, gateway, publisher, cancellationToken);
                    break;
                default:
                    message.MarkFailed($"Unsupported outbox message type '{message.Type}'.");
                    logger.LogError("Unsupported outbox message type {MessageType} for {MessageId}", message.Type, message.Id);
                    break;
            }
        }
        catch (Exception ex)
        {
            message.MarkFailed(ex.Message);
            logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
        }
    }

    /// <summary>
    /// PaymentInitiated mesajini charge gateway cagrisiyle isler.
    /// Basarida Payment Completed olur; dead letter durumunda Payment Failed ve PaymentFailed event'i olusur.
    /// </summary>
    private async Task ProcessPaymentInitiatedAsync(
        OutboxMessage message,
        PaymentDbContext db,
        IPaymentGateway gateway,
        IPublisher publisher,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<PaymentInitiatedOutboxPayload>(message.Payload)
            ?? throw new InvalidOperationException("Payment initiated payload could not be deserialized.");

        var request = new PaymentRequest(
            payload.PaymentId,
            payload.TicketId,
            payload.Amount,
            payload.Currency,
            payload.Provider);

        var result = await gateway.ChargeAsync(request, cancellationToken);
        var payment = await db.Payments.SingleOrDefaultAsync(
            item => item.Id == payload.PaymentId,
            cancellationToken);

        if (result.IsSuccess)
        {
            payment?.Complete(result.Value!);
            message.MarkProcessed();

            await publisher.Publish(
                new PaymentCompleted(payload.PaymentId, payload.TicketId, payload.UserId),
                cancellationToken);

            logger.LogInformation("Payment completed: {PaymentId}", payload.PaymentId);
            return;
        }

        message.MarkFailed(result.Error?.Message ?? "Payment gateway failed.");

        if (message.IsDeadLetter(_settings.MaxRetryCount))
        {
            payment?.Fail();

            await publisher.Publish(
                new PaymentFailed(payload.PaymentId, payload.TicketId, payload.UserId),
                cancellationToken);

            logger.LogCritical(
                "Payment dead letter: {PaymentId} after {RetryCount} retries",
                payload.PaymentId,
                message.RetryCount);
        }
    }

    /// <summary>
    /// PaymentRefundRequested mesajini refund gateway cagrisiyle isler.
    /// Basarili iade sonucunda Payment Refunded olur ve PaymentRefunded event'i yayinlanir.
    /// </summary>
    private async Task ProcessRefundRequestedAsync(
        OutboxMessage message,
        PaymentDbContext db,
        IPaymentGateway gateway,
        IPublisher publisher,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<RefundPaymentOutboxPayload>(message.Payload)
            ?? throw new InvalidOperationException("Refund payment payload could not be deserialized.");

        var result = await gateway.RefundAsync(payload.ExternalPaymentId, cancellationToken);
        var payment = await db.Payments.SingleOrDefaultAsync(
            item => item.Id == payload.PaymentId,
            cancellationToken);

        if (result.IsSuccess)
        {
            payment?.Refund();
            message.MarkProcessed();

            await publisher.Publish(
                new PaymentRefunded(payload.PaymentId, payload.TicketId, payload.UserId),
                cancellationToken);

            logger.LogInformation("Payment refunded: {PaymentId}", payload.PaymentId);
            return;
        }

        message.MarkFailed(result.Error?.Message ?? "Payment refund gateway failed.");

        if (message.IsDeadLetter(_settings.MaxRetryCount))
        {
            logger.LogCritical(
                "Refund dead letter: {PaymentId} after {RetryCount} retries",
                payload.PaymentId,
                message.RetryCount);
        }
    }
}
