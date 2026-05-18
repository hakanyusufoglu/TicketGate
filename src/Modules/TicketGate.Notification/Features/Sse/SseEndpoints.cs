using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TicketGate.Core.Extensions;
using TicketGate.Core.Metrics;
using TicketGate.Notification.Configuration;
using TicketGate.Notification.Domain;

namespace TicketGate.Notification.Features.Sse;

/// <summary>
/// Server-Sent Events endpoint'lerini map eder.
/// Iletisim tek yonlu oldugu icin WebSocket yerine SSE kullanilir; Redis Pub/Sub coklu instance fan-out saglar.
/// Her baglanti ilgili Redis kanalina subscribe olur ve heartbeat ile proxy timeout riskini azaltir.
/// </summary>
public static class SseEndpoints
{
    private const string LastEventIdHeader = "Last-Event-ID";
    private const string DefaultSseEventName = "message";

    /// <summary>
    /// Ticket ve kullanici bildirim stream endpoint'lerini kaydeder.
    /// Endpoint'ler sadece HTTP/SSE donusumunu yapar; bildirim uretimi SsePublisher tarafindadir.
    /// </summary>
    public static void MapSseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/sse")
            .WithTags("SSE")
            .RequireAuthorization();

        group.MapGet("/ticket/{ticketId:guid}", async Task<IResult> (
            Guid ticketId,
            IConnectionMultiplexer redis,
            IOptions<SseSettings> sseSettings,
            HttpContext context,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("TicketGate.Notification.Sse");
            try
            {
                await StreamEventsAsync(
                    context,
                    redis,
                    [SseChannels.SeatStatus(ticketId)],
                    sseSettings.Value,
                    logger,
                    cancellationToken);

                return Results.Empty;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return Results.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ticket SSE stream failed for ticket {TicketId}", ticketId);
                return context.Response.HasStarted
                    ? Results.Empty
                    : Results.Problem(statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("TicketStatusStream")
        .WithSummary("Koltuk durum degisikliklerini stream eder")
        .WithDescription("""
            Belirtilen ticket icin SSE stream acar.
            seat_status_changed eventi reserved, available ve confirmed durumlarini tasir.
            Last-Event-ID header'i event id sayacini devam ettirmek icin okunur; Redis Pub/Sub gecmis mesaj replay etmez.
            """)
        .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/user", async Task<IResult> (
            IConnectionMultiplexer redis,
            IOptions<SseSettings> sseSettings,
            HttpContext context,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("TicketGate.Notification.Sse");
            try
            {
                var userId = context.GetUserId();
                await StreamEventsAsync(
                    context,
                    redis,
                    [
                        SseChannels.QueueTurn(userId),
                        SseChannels.PaymentConfirmed(userId)
                    ],
                    sseSettings.Value,
                    logger,
                    cancellationToken);

                return Results.Empty;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "User SSE stream rejected because user id claim is missing");
                return context.Response.HasStarted
                    ? Results.Empty
                    : Results.Unauthorized();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return Results.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "User SSE stream failed");
                return context.Response.HasStarted
                    ? Results.Empty
                    : Results.Problem(statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("UserNotificationStream")
        .WithSummary("Kullanici bildirimlerini stream eder")
        .WithDescription("""
            Kullaniciya ozel SSE stream acar.
            your_turn, queue_position ve payment_confirmed eventleri bu stream uzerinden iletilir.
            UserId body veya query'den alinmaz; JWT claim'inden okunur.
            """)
        .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Bir veya daha fazla Redis kanalini SSE response'a baglar.
    /// Her Pub/Sub mesajini id, event ve data alanlariyla yazar; heartbeat araligi SseSettings'ten okunur.
    /// </summary>
    private static async Task StreamEventsAsync(
        HttpContext context,
        IConnectionMultiplexer redis,
        IReadOnlyCollection<string> channelNames,
        SseSettings sseSettings,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        var subscriber = redis.GetSubscriber();
        var channels = channelNames
            .Select(channel => new RedisChannel(channel, RedisChannel.PatternMode.Literal))
            .ToArray();
        var writeLock = new SemaphoreSlim(1, 1);
        var lastEventId = ReadLastEventId(context);

        try
        {
            TicketGateMetrics.ActiveSseConnections.Inc();

            foreach (var channel in channels)
            {
                await subscriber.SubscribeAsync(channel, async (_, message) =>
                {
                    await WriteMessageAsync(
                        context,
                        writeLock,
                        Interlocked.Increment(ref lastEventId),
                        message.ToString(),
                        cancellationToken);
                });
            }

            await RunHeartbeatAsync(
                context,
                writeLock,
                TimeSpan.FromSeconds(sseSettings.HeartbeatIntervalSeconds),
                cancellationToken);
        }
        finally
        {
            TicketGateMetrics.ActiveSseConnections.Dec();

            foreach (var channel in channels)
            {
                await subscriber.UnsubscribeAsync(channel);
            }

            writeLock.Dispose();
            logger.LogDebug("SSE stream unsubscribed from {ChannelCount} Redis channels", channels.Length);
        }
    }

    /// <summary>
    /// SSE heartbeat dongusunu calistirir.
    /// Proxy ve load balancer idle timeout'larini engellemek icin ayarli aralikta comment frame yazar.
    /// </summary>
    private static async Task RunHeartbeatAsync(
        HttpContext context,
        SemaphoreSlim writeLock,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await writeLock.WaitAsync(cancellationToken);
            try
            {
                await context.Response.WriteAsync(": heartbeat\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
            finally
            {
                writeLock.Release();
            }

            await Task.Delay(interval, cancellationToken);
        }
    }

    /// <summary>
    /// Redis Pub/Sub mesajini SSE frame formatinda response'a yazar.
    /// Payload icindeki type alani event adina cevrilir; yoksa message event'i kullanilir.
    /// </summary>
    private static async Task WriteMessageAsync(
        HttpContext context,
        SemaphoreSlim writeLock,
        int eventId,
        string payload,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        await writeLock.WaitAsync(cancellationToken);
        try
        {
            var eventName = GetEventName(payload);
            var data = $"id: {eventId}\nevent: {eventName}\ndata: {payload}\n\n";
            await context.Response.WriteAsync(data, cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
        }
        finally
        {
            writeLock.Release();
        }
    }

    /// <summary>
    /// Last-Event-ID header'ini okur ve gecersiz degerlerde sifirdan baslar.
    /// Redis Pub/Sub kalici olmadigi icin bu deger replay degil, yalnizca sonraki id sayaci icin kullanilir.
    /// </summary>
    private static int ReadLastEventId(HttpContext context)
    {
        return context.Request.Headers.TryGetValue(LastEventIdHeader, out var value) &&
            int.TryParse(value.ToString(), out var eventId)
            ? eventId
            : 0;
    }

    /// <summary>
    /// JSON payload icinden SSE event adini okur.
    /// Payload bozuksa stream'i dusurmeden varsayilan event adi kullanilir.
    /// </summary>
    private static string GetEventName(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            return document.RootElement.TryGetProperty("type", out var typeProperty)
                ? typeProperty.GetString() ?? DefaultSseEventName
                : DefaultSseEventName;
        }
        catch (JsonException)
        {
            return DefaultSseEventName;
        }
    }
}
