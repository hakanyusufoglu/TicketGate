using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketGate.Core.Contracts;

namespace TicketGate.Booking;

public sealed class BookingModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration config)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }
}
