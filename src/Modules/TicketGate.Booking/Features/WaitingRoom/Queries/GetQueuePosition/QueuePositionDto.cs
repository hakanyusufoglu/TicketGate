namespace TicketGate.Booking.Features.WaitingRoom.Queries.GetQueuePosition;

/// <summary>Kuyruk pozisyon bilgisi. Position kullaniciya 1-indexed olarak doner.</summary>
public sealed record QueuePositionDto(
    Guid EventId,
    Guid UserId,
    long Position,
    long TotalInQueue);
