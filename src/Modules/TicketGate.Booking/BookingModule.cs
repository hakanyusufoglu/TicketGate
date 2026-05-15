using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;
using TicketGate.Booking.Configuration;
using TicketGate.Booking.Features.Tickets.Endpoints;
using TicketGate.Booking.Features.WaitingRoom.Endpoints;
using TicketGate.Booking.Infrastructure;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Booking.Infrastructure.Workers;
using TicketGate.Core.Contracts;

namespace TicketGate.Booking;

/// <summary>
/// Booking modulu kayit sinifi. IModule implementasyonu ile Program.cs'e dokunmadan sisteme dahil olur.
/// Redis baglantisi, DbContext ve validator kayitlari burada yapilir; MediatR merkezi module discovery tarafindan kaydedilir.
/// </summary>
public sealed class BookingModule : IModule
{
    /// <summary>
    /// Booking DbContext, Redis connection multiplexer, worker ve validator kayitlarini ekler.
    /// IConnectionMultiplexer TryAddSingleton ile kaydedilir; baska modul kaydettiyse duplicate connection acilmaz.
    /// </summary>
    public void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Booking");

        services.AddDbContext<BookingDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", BookingSchema.Name);
            });
            options.UseSnakeCaseNamingConvention();
        });

        var redisConnectionString = config.GetConnectionString("Redis");
        services.TryAddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString ?? "localhost:6379"));

        services.Configure<BookingSettings>(config.GetSection(BookingSettings.SectionName));

        services.AddHostedService<TicketLockExpiredWorker>();
        services.AddHostedService<QueueDispatcher>();

        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly, includeInternalTypes: true);
    }

    /// <summary>
    /// Booking HTTP endpoint'lerini uygulama route tablosuna ekler.
    /// Endpoint dosyalari handler disinda is mantigi tasimaz.
    /// </summary>
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapTicketEndpoints();
        app.MapWaitingRoomEndpoints();
    }
}
