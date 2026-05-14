using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketGate.Core.Contracts;
using TicketGate.Notification.Configuration;

namespace TicketGate.Notification;

public sealed class NotificationModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        services.Configure<SseSettings>(config.GetSection(SseSettings.SectionName));
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }
}
