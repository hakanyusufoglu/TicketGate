using FluentAssertions;
using TicketGate.Payment.Infrastructure.Gateways;

namespace TicketGate.Payment.Tests.Infrastructure;

/// <summary>
/// MockPaymentGateway davranisini dogrulayan testler.
/// Development ortaminda Stripe benzeri referans formati uretilmelidir.
/// </summary>
public sealed class MockPaymentGatewayTests
{
    /// <summary>
    /// ChargeAsync basarili sonucunda mock_ch_ prefix'li Stripe benzeri external id donmelidir.
    /// Bu format manuel test ve log takibinde gercek gateway referansina benzerlik saglar.
    /// </summary>
    [Fact]
    public async Task ChargeAsync_ReturnsMockChargeExternalId()
    {
        var gateway = new MockPaymentGateway();
        var request = new PaymentRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            500m,
            "TRY",
            "Stripe");

        var result = await gateway.ChargeAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith("mock_ch_");
        Guid.TryParseExact(result.Value!["mock_ch_".Length..], "N", out _)
            .Should()
            .BeTrue();
    }
}
