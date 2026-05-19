using Mediator;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.Tickets.Queries.GetTicketById;

/// <summary>Id ile tekil bilet sorgulama istegi.</summary>
public sealed record GetTicketByIdQuery(Guid Id) : IRequest<Result<TicketDetailDto>>;
