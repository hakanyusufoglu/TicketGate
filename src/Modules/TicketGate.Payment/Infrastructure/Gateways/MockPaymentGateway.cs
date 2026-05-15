using TicketGate.Core.Results;

namespace TicketGate.Payment.Infrastructure.Gateways;

/// <summary>
/// Stripe odeme gateway'inin development ortami simulasyonudur.
/// Gercek para cekmez; charge icin Stripe benzeri mock_ch_ referansi uretir.
/// Production ortaminda ayni interface arkasinda Stripe implementasyonu ile degistirilebilir.
/// </summary>
public sealed class MockPaymentGateway : IPaymentGateway
{
    /// <summary>
    /// Stripe ChargeAsync simulasyonunu basarili sonuc olarak tamamlar.
    /// ExternalPaymentId mock_ch_{guid} formatinda uretilir ve OutboxWorker tarafindan Payment kaydina yazilir.
    /// </summary>
    public Task<Result<string>> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        var externalPaymentId = $"mock_ch_{Guid.NewGuid():N}";
        return Task.FromResult(Result<string>.Ok(externalPaymentId));
    }

    /// <summary>
    /// Stripe RefundAsync simulasyonunu basarili sonuc olarak tamamlar.
    /// ExternalPaymentId development ortaminda dogrulanmaz; production gateway bu referansla iade baslatir.
    /// </summary>
    public Task<Result> RefundAsync(string externalPaymentId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok());
    }
}
