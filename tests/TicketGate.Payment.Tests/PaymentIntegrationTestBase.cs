using Microsoft.Extensions.DependencyInjection;
using TicketGate.TestInfrastructure;

namespace TicketGate.Payment.Tests;

/// <summary>
/// Payment modülü integration testleri için base sınıf.
/// Outbox ve transaction testleri gerçek PostgreSQL üzerinde çalışacağı için ortak Testcontainers altyapısını kullanır.
/// </summary>
public abstract class PaymentIntegrationTestBase : IntegrationTestBase
{
    /// <summary>
    /// Payment testleri için gerekli servisleri kaydeder.
    /// PaymentDbContext ve outbox servisleri P8 kapsamında domain kodu geldiğinde bu sınıfa eklenecektir.
    /// </summary>
    protected override void ConfigureServices(IServiceCollection services)
    {
    }
}
