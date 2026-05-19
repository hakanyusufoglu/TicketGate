using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TicketGate.Booking.Configuration;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.TestInfrastructure;

namespace TicketGate.Booking.Tests;

/// <summary>
/// Booking modulu integration testleri icin base sinif.
/// Gercek PostgreSQL ve Redis baglantilariyla xmin concurrency ve Redis SETNX davranislari dogrulanir.
/// </summary>
public abstract class BookingIntegrationTestBase : IntegrationTestBase
{
    /// <summary>
    /// Booking testleri icin DbContext, Redis, Mediator ve validator kayitlarini kurar.
    /// Handler testleri production module registration'a bagimli kalmadan gercek altyapi uzerinde calisir.
    /// </summary>
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<BookingDbContext>(options =>
        {
            options.UseNpgsql(PostgresConnectionString);
            options.UseSnakeCaseNamingConvention();
        });

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(RedisConnectionString));

        services.AddSingleton(Options.Create(new BookingSettings()));

        services.AddLogging();

        services.AddMediator(options =>
            options.ServiceLifetime = ServiceLifetime.Scoped);

        services.AddValidatorsFromAssembly(typeof(BookingModule).Assembly, includeInternalTypes: true);
    }

    /// <summary>
    /// Booking migration'larini test veritabanina uygular.
    /// Gercek schema ve xmin token konfigurasyonu olmadan concurrency testleri anlamli sonuc vermez.
    /// </summary>
    protected override async Task ApplyMigrationsAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<BookingDbContext>();
        await db.Database.MigrateAsync();
    }
}
