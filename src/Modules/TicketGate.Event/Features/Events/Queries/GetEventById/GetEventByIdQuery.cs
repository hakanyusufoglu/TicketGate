using MediatR;
using TicketGate.Core.Results;

namespace TicketGate.Event.Features.Events.Queries.GetEventById;

public sealed record GetEventByIdQuery(Guid Id) : IRequest<Result<EventDetailDto>>;
