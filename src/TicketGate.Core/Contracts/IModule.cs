using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TicketGate.Core.Contracts;

public interface IModule
{
    void RegisterServices(IServiceCollection services, IConfiguration config);

    void MapEndpoints(IEndpointRouteBuilder app);
}
