using MediatR;
using TicketGate.Core.Results;

namespace TicketGate.Event.Features.Venues.Commands.CreateVenue;

public sealed record CreateVenueCommand(string Name, string Location, string SeatMap) : IRequest<Result<Guid>>;
