using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;
using TicketGate.Core.Contracts;
using TicketGate.Event.Configuration;
using TicketGate.Event.Infrastructure;
using TicketGate.Event.Features.Events.Endpoints;
using TicketGate.Event.Infrastructure.Cache;
using TicketGate.Event.Infrastructure.Persistence;

namespace TicketGate.Event;

public sealed class EventModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Event");

        services.AddDbContext<EventDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", EventSchema.Name);
            });
            options.UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IEventSeatMapReader, EventSeatMapReader>();
        services.Configure<EventCacheSettings>(config.GetSection(EventCacheSettings.SectionName));
        services.TryAddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(config.GetConnectionString("Redis") ?? "localhost:6379"));
        services.AddScoped<IEventCacheService, EventCacheService>();

        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly, includeInternalTypes: true);
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapEventEndpoints();
    }
}
