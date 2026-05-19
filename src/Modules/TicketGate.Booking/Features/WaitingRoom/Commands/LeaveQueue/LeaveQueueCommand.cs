using Mediator;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.WaitingRoom.Commands.LeaveQueue;

/// <summary>Kullaniciyi waiting room kuyrugundan cikarma istegi.</summary>
public sealed record LeaveQueueCommand(Guid EventId, Guid UserId) : IRequest<Result>;
