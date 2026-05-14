using MediatR;
using TicketGate.Core.Domain;
using TicketGate.Core.Results;

namespace TicketGate.Event.Features.Venues.Commands.CreateVenue;

/// <summary>Mekan olusturma komutu. SeatMap typed value object olarak alinip jsonb kolona yazilir.</summary>
public sealed record CreateVenueCommand(string Name, string Location, SeatMap SeatMap) : IRequest<Result<Guid>>;
