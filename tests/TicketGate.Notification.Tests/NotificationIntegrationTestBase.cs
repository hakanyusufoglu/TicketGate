using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TicketGate.TestInfrastructure;

namespace TicketGate.Notification.Tests;

/// <summary>
/// Notification modulu integration testleri icin ortak test tabani.
/// SSE fan-out dogrulamasi gercek Redis Pub/Sub davranisi uzerinden yapilir.
/// </summary>
public abstract class NotificationIntegrationTestBase : IntegrationTestBase
{
    /// <summary>
    /// Notification testleri icin Redis, logging ve module servislerini kaydeder.
    /// Testler production Pub/Sub davranisina yakin calisir.
    /// </summary>
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(RedisConnectionString));

        services.AddLogging();
        new NotificationModule().RegisterServices(
            services,
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
    }
}
