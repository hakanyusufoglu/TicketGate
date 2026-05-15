using MediatR;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.WaitingRoom.Commands.JoinQueue;

/// <summary>Kullaniciyi waiting room kuyruguna ekleme istegi. EventId ve UserId zorunludur.</summary>
public sealed record JoinQueueCommand(Guid EventId, Guid UserId)
    : IRequest<Result<JoinQueueResponse>>;
