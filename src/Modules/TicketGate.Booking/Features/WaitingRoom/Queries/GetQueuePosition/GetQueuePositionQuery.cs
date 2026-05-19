using Mediator;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.WaitingRoom.Queries.GetQueuePosition;

/// <summary>Kullanicinin waiting room pozisyonunu sorgulama istegi.</summary>
public sealed record GetQueuePositionQuery(Guid EventId, Guid UserId)
    : IRequest<Result<QueuePositionDto>>;
