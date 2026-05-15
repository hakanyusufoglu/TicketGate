using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using TicketGate.Core.Contracts;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using TicketGate.Payment.Configuration;
using TicketGate.Payment.Infrastructure.Gateways;
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
    private readonly FakePaymentGateway _paymentGateway = new();
    private readonly HttpContextAccessor _httpContextAccessor = new();

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
        services.AddSingleton<IHttpContextAccessor>(_httpContextAccessor);
        services.AddSingleton<IPaymentGateway>(_paymentGateway);
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
        _ticketReservationReader.SetReserved(ticketId, userId, 500m);
    }

    /// <summary>
    /// Testlerde ticket reservation bilgisini fiyatla birlikte ayarlar.
    /// InitiatePayment amount bilgisini body yerine bu sozlesmedeki ticket fiyatindan almalidir.
    /// </summary>
    protected void SetReservedTicket(Guid ticketId, Guid userId, decimal price)
    {
        _ticketReservationReader.SetReserved(ticketId, userId, price);
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
    /// Test request'i icin JWT NameIdentifier claim'ini HttpContextAccessor uzerinden kurar.
    /// Payment handler userId bilgisini body yerine bu claim'den okumak zorundadir.
    /// </summary>
    protected void SetCurrentUser(Guid userId)
    {
        _httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                "TestAuth"))
        };
    }

    /// <summary>
    /// Test request'i icin JWT sub claim'ini HttpContextAccessor uzerinden kurar.
    /// Login token'i kullanici id bilgisini standart subject claim'iyle tasidigi icin handler bunu okuyabilmelidir.
    /// </summary>
    protected void SetCurrentUserSubject(Guid userId)
    {
        _httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", userId.ToString())],
                "TestAuth"))
        };
    }

    /// <summary>
    /// Test request'i icin kimlik bilgisini temizler.
    /// Unauthorized senaryosu body'den userId kabul edilmedigini dogrulamak icin kullanilir.
    /// </summary>
    protected void ClearCurrentUser()
    {
        _httpContextAccessor.HttpContext = new DefaultHttpContext();
    }

    /// <summary>
    /// Fake gateway'in bir sonraki charge sonucunu basarili olacak sekilde ayarlar.
    /// OutboxWorker testleri harici gateway bagimliligi olmadan odeme tamamlama akisini dogrular.
    /// </summary>
    protected void SetGatewayChargeSuccess(string externalPaymentId)
    {
        _paymentGateway.NextChargeResult = Result<string>.Ok(externalPaymentId);
    }

    /// <summary>
    /// Fake gateway'in bir sonraki charge sonucunu basarisiz olacak sekilde ayarlar.
    /// Retry ve dead letter davranislari Result.Fail uzerinden test edilir.
    /// </summary>
    protected void SetGatewayChargeFailure(string message)
    {
        _paymentGateway.NextChargeResult = Result<string>.Fail(AppError.Conflict("payment.gateway_failed", message));
    }

    /// <summary>
    /// Payment testlerinde kullanilan kontrollu ticket reservation okuyucusudur.
    /// Cross-module bagimliligi gercek Booking modulu yerine Core contract uzerinden taklit eder.
    /// </summary>
    private sealed class FakeTicketReservationReader : ITicketReservationReader
    {
        private readonly Dictionary<Guid, TicketReservationInfo> _reservedTickets = new();

        /// <summary>
        /// Ticket'i belirli kullanici icin reserved kabul eder.
        /// Idempotency testleri ayni ticket durumunu tekrar kullanir.
        /// </summary>
        public void SetReserved(Guid ticketId, Guid userId, decimal price)
        {
            _reservedTickets[ticketId] = new TicketReservationInfo(ticketId, userId, price);
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
            return Task.FromResult(_reservedTickets.TryGetValue(ticketId, out var info)
                ? Result<TicketReservationInfo>.Ok(info)
                : Result<TicketReservationInfo>.Fail(TicketReservationErrors.NotReserved(ticketId)));
        }
    }

    /// <summary>
    /// OutboxWorker integration testleri icin kontrollu payment gateway fake'idir.
    /// Testler gateway basari veya hata sonucunu explicit olarak belirler.
    /// </summary>
    private sealed class FakePaymentGateway : IPaymentGateway
    {
        public Result<string> NextChargeResult { get; set; } = Result<string>.Ok(Guid.NewGuid().ToString());

        /// <summary>
        /// Ayarlanan charge sonucunu doner.
        /// Gercek harici servis cagrisi yapmadan worker retry kararini test etmeyi saglar.
        /// </summary>
        public Task<Result<string>> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(NextChargeResult);
        }

        /// <summary>
        /// Test kapsaminda iade gateway davranisini basarili kabul eder.
        /// Refund worker akisi bu test sinifinda hedeflenmez.
        /// </summary>
        public Task<Result> RefundAsync(string externalPaymentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Ok());
        }
    }
}
