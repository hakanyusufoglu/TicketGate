using Mediator;
using TicketGate.Core.Results;

namespace TicketGate.Event.Features.Venues.Queries.GetVenueById;

public sealed record GetVenueByIdQuery(Guid Id) : IRequest<Result<VenueDetailDto>>;
