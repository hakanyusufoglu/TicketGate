using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketGate.Core.Contracts;

namespace TicketGate.Payment;

public sealed class PaymentModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration config)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }
}
