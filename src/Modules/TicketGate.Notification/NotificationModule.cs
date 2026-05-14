using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketGate.Core.Contracts;

namespace TicketGate.Notification;

public sealed class NotificationModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration config)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }
}
