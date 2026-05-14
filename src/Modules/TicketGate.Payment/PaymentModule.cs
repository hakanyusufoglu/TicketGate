using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketGate.Core.Contracts;
using TicketGate.Payment.Configuration;

namespace TicketGate.Payment;

public sealed class PaymentModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        services.Configure<OutboxSettings>(config.GetSection(OutboxSettings.SectionName));
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }
}
