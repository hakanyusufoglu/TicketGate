using FluentAssertions;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TicketGate.Booking.Configuration;
using TicketGate.Booking.Features.WaitingRoom.Commands.JoinQueue;
using TicketGate.Booking.Features.WaitingRoom.Commands.LeaveQueue;
using TicketGate.Booking.Features.WaitingRoom.Queries.GetQueuePosition;
using TicketGate.Booking.Infrastructure.Workers;
using TicketGate.Core.Errors;

namespace TicketGate.Booking.Tests.Features;

/// <summary>
/// Virtual Waiting Room integration testleri.
/// Gercek Redis Sorted Set davranisi ve Pub/Sub bildirim akisi dogrulanir.
/// </summary>
public sealed class WaitingRoomTests : BookingIntegrationTestBase
{
    private static readonly TimeSpan AssertionTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan AssertionPollInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Kapasite doluyken kuyruğa giren kullanicinin dogru pozisyonu aldigini dogrular.
    /// Redis ZADD NX ve ZRANK davranisi gercek Redis uzerinde calisir.
    /// </summary>
    [Fact]
    public async Task JoinQueue_WhenCapacityFull_ReturnsPosition()
    {
        await ResetAsync();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var redisDb = Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        await redisDb.StringSetAsync($"active_checkout:{eventId}", new BookingSettings().MaxCheckoutCapacity);

        var result = await SendScopedAsync(new JoinQueueCommand(eventId, userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.EventId.Should().Be(eventId);
        result.Value.UserId.Should().Be(userId);
        result.Value.Position.Should().Be(1);
        result.Value.CanProceedDirectly.Should().BeFalse();
    }

    /// <summary>
    /// Ayni kullanicinin iki kez kuyruğa girmesinde pozisyonun degismedigini dogrular.
    /// ZADD NX ilk giris zamanini korudugu icin tekrar join sirayi bozmaz.
    /// </summary>
    [Fact]
    public async Task JoinQueue_SameUser_PositionUnchanged()
    {
        await ResetAsync();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var redisDb = Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        await redisDb.StringSetAsync($"active_checkout:{eventId}", new BookingSettings().MaxCheckoutCapacity);

        var first = await SendScopedAsync(new JoinQueueCommand(eventId, userId));
        var second = await SendScopedAsync(new JoinQueueCommand(eventId, userId));

        first.Value!.Position.Should().Be(1);
        second.Value!.Position.Should().Be(1);
        var total = await redisDb.SortedSetLengthAsync($"waitingroom:{eventId}");
        total.Should().Be(1);
    }

    /// <summary>
    /// Kapasite bosken kuyruğa giren kullanicinin direkt rezervasyon akişına geçtigini dogrular.
    /// Direct grant aktif checkout sayacini artirarak kapasite kacagini engeller.
    /// </summary>
    [Fact]
    public async Task JoinQueue_WhenCapacityAvailable_ProceedDirectly()
    {
        await ResetAsync();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var result = await SendScopedAsync(new JoinQueueCommand(eventId, userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Position.Should().Be(0);
        result.Value.CanProceedDirectly.Should().BeTrue();

        var redisDb = Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var activeCheckout = await redisDb.StringGetAsync($"active_checkout:{eventId}");
        activeCheckout.ToString().Should().Be("1");
    }

    /// <summary>
    /// Es zamanli direct-join isteklerinde checkout kapasitesinin asilmasini engelledigini dogrular.
    /// Kapasite kontrolu ve active_checkout artisi atomik degilse bu test birden fazla direkt gecis yakalar.
    /// </summary>
    [Fact]
    public async Task JoinQueue_ConcurrentDirectJoins_GrantsOnlyAvailableCapacity()
    {
        await ResetAsync();
        var eventId = Guid.NewGuid();
        var redisDb = Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        await redisDb.StringSetAsync($"active_checkout:{eventId}", new BookingSettings().MaxCheckoutCapacity - 1);

        var requests = Enumerable.Range(0, 20)
            .Select(_ => SendScopedAsync(new JoinQueueCommand(eventId, Guid.NewGuid())));

        var results = await Task.WhenAll(requests);

        results.Count(result => result.Value!.CanProceedDirectly).Should().Be(1);
        results.Count(result => !result.Value!.CanProceedDirectly).Should().Be(19);

        var activeCheckout = await redisDb.StringGetAsync($"active_checkout:{eventId}");
        activeCheckout.ToString().Should().Be(new BookingSettings().MaxCheckoutCapacity.ToString());
    }

    /// <summary>
    /// Kuyruktan cikan kullanicinin pozisyonunun silindigini dogrular.
    /// Ardindan pozisyon sorgusu 404 dondurerek stale queue bilgisini engeller.
    /// </summary>
    [Fact]
    public async Task LeaveQueue_RemovesUserFromQueue()
    {
        await ResetAsync();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var redisDb = Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        await redisDb.StringSetAsync($"active_checkout:{eventId}", new BookingSettings().MaxCheckoutCapacity);
        await SendScopedAsync(new JoinQueueCommand(eventId, userId));

        var leaveResult = await SendScopedAsync(new LeaveQueueCommand(eventId, userId));
        var positionResult = await SendScopedAsync(new GetQueuePositionQuery(eventId, userId));

        leaveResult.IsSuccess.Should().BeTrue();
        positionResult.IsFailure.Should().BeTrue();
        positionResult.Error!.Type.Should().Be(AppErrorType.NotFound);
    }

    /// <summary>
    /// QueueDispatcher calisinca siradaki kullanicinin Redis Pub/Sub kanalindan bildirim aldigini dogrular.
    /// ZPOPMIN adil sirayi korur ve active_checkout sayaci grant sonrasi artar.
    /// </summary>
    [Fact]
    public async Task Dispatcher_NotifiesNextUser_WhenCapacityAvailable()
    {
        await ResetAsync();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var redis = Services.GetRequiredService<IConnectionMultiplexer>();
        var redisDb = redis.GetDatabase();
        await redisDb.SortedSetAddAsync($"waitingroom:{eventId}", userId.ToString(), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var channel = new RedisChannel($"queue:{userId}:turn", RedisChannel.PatternMode.Literal);
        await redis.GetSubscriber().SubscribeAsync(channel, (_, message) => received.TrySetResult(message.ToString()));

        using var dispatcher = new QueueDispatcher(
            redis,
            Options.Create(new BookingSettings
            {
                MaxCheckoutCapacity = 1,
                QueueDispatcherIntervalSeconds = 1,
                QueueDispatchBatchSize = 10
            }),
            Services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<QueueDispatcher>.Instance);

        await dispatcher.StartAsync(CancellationToken.None);

        var message = await WaitForMessageAsync(received);
        message.Should().Contain(eventId.ToString());

        var activeCheckout = await redisDb.StringGetAsync($"active_checkout:{eventId}");
        activeCheckout.ToString().Should().Be("1");

        await dispatcher.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// Dispatcher bir kullaniciya sira verdikten sonra kalan kullanicilarin pozisyonunu guncelledigini dogrular.
    /// ZPOPMIN sonrasi queue_position eventi yayinlanmazsa UI eski sira bilgisini gostermeye devam eder.
    /// </summary>
    [Fact]
    public async Task Dispatcher_PublishesUpdatedPosition_ForRemainingUsers()
    {
        await ResetAsync();
        var eventId = Guid.NewGuid();
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var redis = Services.GetRequiredService<IConnectionMultiplexer>();
        var redisDb = redis.GetDatabase();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await redisDb.SortedSetAddAsync($"waitingroom:{eventId}", firstUserId.ToString(), now);
        await redisDb.SortedSetAddAsync($"waitingroom:{eventId}", secondUserId.ToString(), now + 1);

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var channel = new RedisChannel($"queue:{secondUserId}:turn", RedisChannel.PatternMode.Literal);
        await redis.GetSubscriber().SubscribeAsync(channel, (_, message) => received.TrySetResult(message.ToString()));

        using var dispatcher = new QueueDispatcher(
            redis,
            Options.Create(new BookingSettings
            {
                MaxCheckoutCapacity = 1,
                QueueDispatcherIntervalSeconds = 1,
                QueueDispatchBatchSize = 10
            }),
            Services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<QueueDispatcher>.Instance);

        await dispatcher.StartAsync(CancellationToken.None);

        var message = await WaitForMessageAsync(received);
        message.Should().Contain("queue_position");
        message.Should().Contain(eventId.ToString());
        message.Should().Contain(secondUserId.ToString());
        message.Should().Contain("\"position\":1");
        message.Should().Contain("\"total\":1");

        await dispatcher.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// Request'i yeni DI scope icinde Mediator ile gonderir.
    /// Handler testleri production request scope davranisina yakin calisir.
    /// </summary>
    private async Task<TicketGate.Core.Results.Result<TResponse>> SendScopedAsync<TResponse>(
        IRequest<TicketGate.Core.Results.Result<TResponse>> request)
    {
        using var scope = Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await sender.Send(request);
    }

    /// <summary>
    /// Non-generic Result donduren command'i yeni DI scope icinde gonderir.
    /// LeaveQueue gibi yazma islemleri ayni test izolasyonunu kullanir.
    /// </summary>
    private async Task<TicketGate.Core.Results.Result> SendScopedAsync(
        IRequest<TicketGate.Core.Results.Result> request)
    {
        using var scope = Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await sender.Send(request);
    }

    /// <summary>
    /// Pub/Sub mesaji gelene kadar polling yapar.
    /// Sabit uyku yerine timeout kontrollu bekleme test flakiness riskini azaltir.
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

        throw new TimeoutException("Queue turn notification was not received.");
    }
}
