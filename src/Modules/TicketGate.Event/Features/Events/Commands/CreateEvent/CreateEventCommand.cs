using MediatR;
using TicketGate.Core.Results;

namespace TicketGate.Event.Features.Events.Commands.CreateEvent;

public sealed record CreateEventCommand(
    string Name,
    string Description,
    Guid VenueId,
    Guid PerformerId,
    DateTime StartsAt,
    DateTime EndsAt) : IRequest<Result<Guid>>;
