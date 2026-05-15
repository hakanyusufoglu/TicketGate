namespace TicketGate.Booking.Features.WaitingRoom.Commands.JoinQueue;

/// <summary>Kuyruga katilim yaniti. Position 0 ise kullanici direkt rezervasyona gecebilir.</summary>
public sealed record JoinQueueResponse(
    Guid EventId,
    Guid UserId,
    long Position,
    bool CanProceedDirectly);
