using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketGate.Core.Contracts;
using TicketGate.Payment.Configuration;
using TicketGate.Payment.Features.Payments.Endpoints;
using TicketGate.Payment.Infrastructure;
using TicketGate.Payment.Infrastructure.Gateways;
using TicketGate.Payment.Infrastructure.Persistence;
using TicketGate.Payment.Infrastructure.Workers;

namespace TicketGate.Payment;

/// <summary>
/// Payment modulu kayit sinifi. IModule implementasyonu ile Program.cs'e dokunmadan sisteme dahil olur.
/// Development ortaminda MockPaymentGateway kaydedilir; harici gateway cagrisi handler'larda yapilmaz.
/// </summary>
public sealed class PaymentModule : IModule
{
    /// <summary>
    /// Payment DbContext, OutboxSettings, gateway soyutlamasi ve validator kayitlarini ekler.
    /// MediatR merkezi module discovery tarafindan kaydedildigi icin burada tekrar pipeline eklenmez.
    /// </summary>
    public void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Payment");

        services.AddDbContext<PaymentDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", PaymentSchema.Name);
            });
            options.UseSnakeCaseNamingConvention();
        });

        services.Configure<OutboxSettings>(config.GetSection(OutboxSettings.SectionName));

        services.AddHttpContextAccessor();
        services.AddScoped<IPaymentGateway, MockPaymentGateway>();
        services.AddHostedService<OutboxWorker>();
        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly, includeInternalTypes: true);
    }

    /// <summary>
    /// Payment HTTP endpoint'lerini uygulama route tablosuna ekler.
    /// Endpoint dosyalari handler disinda is mantigi tasimaz.
    /// </summary>
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPaymentEndpoints();
    }
}
