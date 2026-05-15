using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using TicketGate.Core.Events;
using TicketGate.Notification.Domain;
using TicketGate.Notification.Infrastructure;

namespace TicketGate.Notification.Tests;

/// <summary>
/// SsePublisher Redis Pub/Sub fan-out testleri.
/// Domain event geldigi anda dogru kanala JSON payload yayinlanmasi dogrulanir.
/// </summary>
public sealed class SsePublisherTests : NotificationIntegrationTestBase
{
    private static readonly TimeSpan AssertionTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan AssertionPollInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// TicketReserved event'i seat:{ticketId}:status kanalina seat_status_changed payload'u yayinlamalidir.
    /// Bu akis koltuk ekranlarinin coklu instance senaryosunda Redis fan-out ile guncellenmesini saglar.
    /// </summary>
    [Fact]
    public async Task Handle_TicketReserved_PublishesSeatStatusChanged()
    {
        await ResetAsync();
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var redis = Services.GetRequiredService<IConnectionMultiplexer>();
        var received = await SubscribeAsync(redis, SseChannels.SeatStatus(ticketId));
        var publisher = new SsePublisher(redis, NullLogger<SsePublisher>.Instance);

        await publisher.Handle(
            new TicketReserved(ticketId, Guid.NewGuid(), "A-1", 100m, userId, DateTime.UtcNow.AddMinutes(10)),
            CancellationToken.None);

        var message = await WaitForMessageAsync(received);
        using var document = JsonDocument.Parse(message);
        var root = document.RootElement;
        root.GetProperty("type").GetString().Should().Be(SseEventTypes.SeatStatusChanged);
        root.GetProperty("ticketId").GetGuid().Should().Be(ticketId);
        root.GetProperty("seat").GetString().Should().Be("A-1");
        root.GetProperty("status").GetString().Should().Be("reserved");
    }

    /// <summary>
    /// QueuePositionPublisher kullaniciya ozel queue kanalina queue_position payload'u yayinlamalidir.
    /// Bu bildirim bekleme odasi UI'inin polling yapmadan pozisyon gostermesini saglar.
    /// </summary>
    [Fact]
    public async Task PublishPositionAsync_PublishesQueuePosition()
    {
        await ResetAsync();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var redis = Services.GetRequiredService<IConnectionMultiplexer>();
        var received = await SubscribeAsync(redis, SseChannels.QueueTurn(userId));
        var publisher = new QueuePositionPublisher(redis, NullLogger<QueuePositionPublisher>.Instance);

        await publisher.PublishPositionAsync(userId, eventId, position: 3, total: 8, CancellationToken.None);

        var message = await WaitForMessageAsync(received);
        using var document = JsonDocument.Parse(message);
        var root = document.RootElement;
        root.GetProperty("type").GetString().Should().Be(SseEventTypes.QueuePosition);
        root.GetProperty("eventId").GetGuid().Should().Be(eventId);
        root.GetProperty("userId").GetGuid().Should().Be(userId);
        root.GetProperty("position").GetInt64().Should().Be(3);
        root.GetProperty("total").GetInt64().Should().Be(8);
    }

    /// <summary>
    /// Test Redis subscriber'ini verilen kanala baglar ve ilk mesaji yakalar.
    /// Subscribe tamamlanmadan publish edilirse Pub/Sub mesaj kaybolacagi icin await edilir.
    /// </summary>
    private static async Task<TaskCompletionSource<string>> SubscribeAsync(
        IConnectionMultiplexer redis,
        string channelName)
    {
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var channel = new RedisChannel(channelName, RedisChannel.PatternMode.Literal);
        await redis.GetSubscriber().SubscribeAsync(channel, (_, message) => received.TrySetResult(message.ToString()));
        return received;
    }

    /// <summary>
    /// Redis Pub/Sub mesajini timeout ile bekler.
    /// Sabit uyku yerine kontrollu polling kullanildigi icin test flakiness riski azalir.
    /// </summary>
    private static async Task<string> WaitForMessageAsync(TaskCompletionSource<string> source)
    {
        var deadline = DateTime.UtcNow.Add(AssertionTimeout);
        while (DateTime.UtcNow < deadline)
        {
            if (source.Task.IsCompletedSuccessfully)
            {
                return await source.Task;
            }

            await Task.Delay(AssertionPollInterval);
        }

        throw new TimeoutException("SSE Redis notification was not received.");
    }
}
