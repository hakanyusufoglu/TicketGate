using TicketGate.Core.Results;

namespace TicketGate.Payment.Infrastructure.Gateways;

/// <summary>
/// Odeme gateway soyutlama arayuzu. Stripe, PayPal ve Mock implementasyonlari bu interface'i implement eder.
/// Dis servise bagimliligi interface arkasina alarak test edilebilirlik ve degistirilebilirlik saglanir.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Odeme islemini gerceklestirir.
    /// Basarisiz olursa Result.Fail doner; OutboxWorker retry kararini Result'a gore verir.
    /// </summary>
    Task<Result<string>> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Iade islemini gerceklestirir.
    /// ExternalPaymentId harici gateway referansidir.
    /// </summary>
    Task<Result> RefundAsync(string externalPaymentId, CancellationToken cancellationToken);
}

/// <summary>Odeme gateway'e gonderilecek istek modeli.</summary>
public sealed record PaymentRequest(
    Guid PaymentId,
    Guid TicketId,
    decimal Amount,
    string Currency,
    string Provider);

/// <summary>
/// Test ve gelistirme ortami icin sahte gateway.
/// Her istekte basarili sonuc doner; gercek odeme yapilmaz.
/// </summary>
public sealed class MockPaymentGateway : IPaymentGateway
{
    /// <summary>
    /// Sahte basarili odeme yapar.
    /// ExternalPaymentId olarak rastgele Guid doner.
    /// </summary>
    public Task<Result<string>> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok(Guid.NewGuid().ToString()));
    }

    /// <summary>
    /// Sahte basarili iade sonucu doner.
    /// Harici servis cagrisi yapmadan development akisini tamamlar.
    /// </summary>
    public Task<Result> RefundAsync(string externalPaymentId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok());
    }
}
