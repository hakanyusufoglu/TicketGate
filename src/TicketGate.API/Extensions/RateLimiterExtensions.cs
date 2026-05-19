using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TicketGate.API.Configuration;
using TicketGate.Core.Security;

namespace TicketGate.API.Extensions;

/// <summary>
/// ASP.NET Core built-in rate limiting yapılandırması.
/// Ocelot Gateway kullanılmadığından rate limiting uygulama katmanında yapılır.
/// Her endpoint grubu için ayrı policy tanımlanır.
/// IP bazlı limitleme ile brute force ve spam korunması sağlanır.
/// </summary>
public static class RateLimiterExtensions
{
    /// <summary>
    /// Rate limiter servislerini appsettings tabanlı policy'lerle kaydeder.
    /// Fixed-window limiter partition key olarak client IP kullandığı için limitler kullanıcılar arasında paylaşılmaz.
    /// </summary>
    public static IServiceCollection AddTicketGateRateLimiter(
        this IServiceCollection services,
        IConfiguration config)
    {
        var settings = config
            .GetSection(RateLimitingSettings.SectionName)
            .Get<RateLimitingSettings>() ?? new RateLimitingSettings();

        services.Configure<RateLimitingSettings>(
            config.GetSection(RateLimitingSettings.SectionName));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (ctx, ct) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await ctx.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        type = "https://ticketgate.io/errors/rate-limit-exceeded",
                        title = "RateLimit.Exceeded",
                        detail = "Çok fazla istek gönderdiniz. Lütfen bekleyin.",
                        status = StatusCodes.Status429TooManyRequests
                    },
                    ct);
            };

            options.AddPolicy(RateLimitPolicies.Auth, context =>
                CreateIpFixedWindowLimiter(context, settings.Auth));

            options.AddPolicy(RateLimitPolicies.Reserve, context =>
                CreateIpFixedWindowLimiter(context, settings.Reserve));

            options.AddPolicy(RateLimitPolicies.Queue, context =>
                CreateIpFixedWindowLimiter(context, settings.Queue));

            options.AddPolicy(RateLimitPolicies.Read, context =>
                CreateIpFixedWindowLimiter(context, settings.Read));

            options.AddPolicy(RateLimitPolicies.Sse, context =>
                CreateIpFixedWindowLimiter(context, settings.Sse));
        });

        return services;
    }

    /// <summary>
    /// Client IP adresine göre partition edilmiş fixed-window limiter üretir.
    /// Proxy arkasında gerçek IP ileride trusted forwarded header konfigürasyonu ile netleştirilebilir.
    /// </summary>
    private static RateLimitPartition<string> CreateIpFixedWindowLimiter(
        HttpContext context,
        RateLimitPolicySettings settings)
    {
        var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = settings.PermitLimit,
                Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = settings.QueueLimit
            });
    }
}
