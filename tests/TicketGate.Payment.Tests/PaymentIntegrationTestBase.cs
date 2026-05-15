using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TicketGate.Core.Contracts;
using TicketGate.Core.Results;
using TicketGate.Payment.Configuration;
using TicketGate.Payment.Infrastructure.Persistence;
using TicketGate.TestInfrastructure;

namespace TicketGate.Payment.Tests;

/// <summary>
/// Payment modulu integration testleri icin base sinif.
/// Outbox ve transaction testleri gercek PostgreSQL uzerinde calisacagi icin ortak Testcontainers altyapisini kullanir.
/// </summary>
public abstract class PaymentIntegrationTestBase : IntegrationTestBase
{
    private readonly FakeTicketReservationReader _ticketReservationReader = new();

    /// <summary>
    /// Payment testleri icin gerekli servisleri kaydeder.
    /// PaymentDbContext, MediatR ve fake ticket reservation reader gercek PostgreSQL uzerinde outbox davranisini dogrular.
    /// </summary>
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<PaymentDbContext>(options =>
        {
            options.UseNpgsql(PostgresConnectionString);
            options.UseSnakeCaseNamingConvention();
        });

        services.AddSingleton(Options.Create(new OutboxSettings()));
        services.AddSingleton<ITicketReservationReader>(_ticketReservationReader);
        services.AddLogging();

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(PaymentModule).Assembly));

        services.AddValidatorsFromAssembly(typeof(PaymentModule).Assembly, includeInternalTypes: true);
    }

    /// <summary>
    /// Payment migration'larini test veritabanina uygular.
    /// Unique idempotency index ve outbox tablosu olmadan testler gercek davranisi olcemez.
    /// </summary>
    protected override async Task ApplyMigrationsAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<PaymentDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>
    /// Testlerde ticket reservation bilgisini kontrol edilebilir hale getirir.
    /// Payment modulu Booking DbContext'e direkt baglanmadigi icin sozlesme fake'i kullanilir.
    /// </summary>
    protected void SetReservedTicket(Guid ticketId, Guid userId)
    {
        _ticketReservationReader.SetReserved(ticketId, userId);
    }

    /// <summary>
    /// Testlerde ticket'i reserved olmayan durumda modellemek icin fake sozlesme durumunu temizler.
    /// Handler'in 409 davranisi bu yol ile dogrulanir.
    /// </summary>
    protected void ClearReservedTickets()
    {
        _ticketReservationReader.Clear();
    }

    /// <summary>
    /// Payment testlerinde kullanilan kontrollu ticket reservation okuyucusudur.
    /// Cross-module bagimliligi gercek Booking modulu yerine Core contract uzerinden taklit eder.
    /// </summary>
    private sealed class FakeTicketReservationReader : ITicketReservationReader
    {
        private readonly Dictionary<Guid, Guid> _reservedTickets = new();

        /// <summary>
        /// Ticket'i belirli kullanici icin reserved kabul eder.
        /// Idempotency testleri ayni ticket durumunu tekrar kullanir.
        /// </summary>
        public void SetReserved(Guid ticketId, Guid userId)
        {
            _reservedTickets[ticketId] = userId;
        }

        /// <summary>
        /// Tum reserved ticket kayitlarini temizler.
        /// Her test senaryosu kendi durumunu acikca kurar.
        /// </summary>
        public void Clear()
        {
            _reservedTickets.Clear();
        }

        /// <summary>
        /// Payment handler'a ticket'in reserved ve user sahibi bilgisini doner.
        /// Olumsuz durumda exception yerine Result.Fail kullanilir.
        /// </summary>
        public Task<Result<TicketReservationInfo>> GetReservedTicketAsync(
            Guid ticketId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_reservedTickets.TryGetValue(ticketId, out var userId)
                ? Result<TicketReservationInfo>.Ok(new TicketReservationInfo(ticketId, userId))
                : Result<TicketReservationInfo>.Fail(TicketReservationErrors.NotReserved(ticketId)));
        }
    }
}
