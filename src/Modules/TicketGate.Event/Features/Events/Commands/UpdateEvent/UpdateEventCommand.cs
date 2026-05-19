using Mediator;
using TicketGate.Core.Results;

namespace TicketGate.Event.Features.Events.Commands.UpdateEvent;

public sealed record UpdateEventCommand(
    Guid Id,
    string Name,
    string Description,
    DateTime StartsAt,
    DateTime EndsAt) : IRequest<Result>;
