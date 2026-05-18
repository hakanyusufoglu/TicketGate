using FluentAssertions;
using Prometheus;
using System.Text;
using TicketGate.Core.Metrics;

namespace TicketGate.Booking.Tests;

public sealed class TicketGateMetricsTests
{
    [Fact]
    public async Task Ticket_reservation_metric_uses_ticketgate_prefix_and_status_label()
    {
        TicketGateMetrics.TicketReservations
            .WithLabels("success")
            .Inc();

        await using var stream = new MemoryStream();
        await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
        var collectedMetrics = Encoding.UTF8.GetString(stream.ToArray());

        collectedMetrics.Should().Contain("ticketgate_ticket_reservations_total");
        collectedMetrics.Should().Contain("status=\"success\"");
    }
}
