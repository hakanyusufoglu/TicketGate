using Serilog.Context;

namespace TicketGate.API.Middleware;

/// <summary>
/// Her HTTP isteğine X-Correlation-Id header'ı ekler.
/// Header yoksa yeni Guid üretir ve değeri LogContext'e taşır.
/// Dağıtık sistemlerde istek takibi için response header'a da aynı değeri yazar.
/// </summary>
public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    /// <summary>
    /// İstek header'ından CorrelationId okur veya yeni üretir.
    /// LogContext'e ekleyerek structured log satırlarında aynı takip değerini görünür kılar.
    /// </summary>
    public async Task InvokeAsync(HttpContext ctx)
    {
        var correlationId = ctx.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        ctx.Items[CorrelationIdHeader] = correlationId;
        ctx.Response.Headers[CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            logger.LogDebug(
                "HTTP request correlation id attached for {Method} {Path}",
                ctx.Request.Method,
                ctx.Request.Path.Value);

            await next(ctx);
        }
    }
}
