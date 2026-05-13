using TicketGate.Core.Events;

namespace TicketGate.Event.Tests;

public sealed class DomainEventTests
{
    [Fact]
    public void Constructor_ShouldAssignEventIdAndOccurredAt()
    {
        var before = DateTime.UtcNow;

        var domainEvent = new TicketReservedDomainEvent(Guid.NewGuid());

        Assert.NotEqual(Guid.Empty, domainEvent.EventId);
        Assert.InRange(domainEvent.OccurredAt, before, DateTime.UtcNow);
    }

    private sealed record TicketReservedDomainEvent(Guid TicketId) : DomainEvent;
}
