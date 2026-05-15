using FluentAssertions;
using TicketGate.Notification.Domain;

namespace TicketGate.Notification.Tests;

/// <summary>
/// SSE Redis kanal isimlendirme testleri.
/// Client ve publisher ayni kanal contract'ina bagli oldugu icin format geriye uyumlu kalmalidir.
/// </summary>
public sealed class SseChannelsTests
{
    /// <summary>
    /// Ticket ve kullanici bazli kanallar beklenen Redis Pub/Sub formatinda uretilmelidir.
    /// Format bozulursa SSE subscriber dogru mesaji alamaz.
    /// </summary>
    [Fact]
    public void Channels_UseExpectedRedisNames()
    {
        var ticketId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        SseChannels.SeatStatus(ticketId).Should().Be($"seat:{ticketId}:status");
        SseChannels.QueueTurn(userId).Should().Be($"queue:{userId}:turn");
        SseChannels.PaymentConfirmed(userId).Should().Be($"payment:{userId}:confirmed");
    }
}
