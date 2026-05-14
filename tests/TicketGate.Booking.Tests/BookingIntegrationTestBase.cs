using Microsoft.Extensions.DependencyInjection;
using TicketGate.TestInfrastructure;

namespace TicketGate.Booking.Tests;

/// <summary>
/// Booking modülü integration testleri için base sınıf.
/// Modül henüz domain servislerini içermediği için şimdilik ortak PostgreSQL ve Redis bağlantılarını sağlar.
/// </summary>
public abstract class BookingIntegrationTestBase : IntegrationTestBase
{
    /// <summary>
    /// Booking testleri için gerekli servisleri kaydeder.
    /// BookingDbContext ve MediatR kayıtları P5 kapsamında domain kodu geldiğinde bu sınıfa eklenecektir.
    /// </summary>
    protected override void ConfigureServices(IServiceCollection services)
    {
    }
}
