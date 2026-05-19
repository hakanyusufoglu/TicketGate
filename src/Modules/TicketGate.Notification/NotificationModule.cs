using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketGate.Core.Contracts;
using TicketGate.Notification.Configuration;
using TicketGate.Notification.Features.Sse;
using TicketGate.Notification.Infrastructure;

namespace TicketGate.Notification;

/// <summary>
/// Notification modulu kayit sinifi.
/// SSE endpoint'leri, SseSettings ve Redis Pub/Sub publisher servisleri burada module pipeline'ina eklenir.
/// </summary>
public sealed class NotificationModule : IModule
{
    /// <summary>
    /// Notification servislerini DI container'a kaydeder.
    /// Mediator handler kaydi merkezi AddModules tarafindan yapildigi icin burada duplicate pipeline eklenmez.
    /// </summary>
    public void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        services.Configure<SseSettings>(config.GetSection(SseSettings.SectionName));
        services.AddScoped<QueuePositionPublisher>();
    }

    /// <summary>
    /// Notification HTTP endpoint'lerini Minimal API route agacina ekler.
    /// SSE stream'leri Redis Pub/Sub kanallarini dinleyerek client'a aktarilir.
    /// </summary>
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapSseEndpoints();
    }
}
