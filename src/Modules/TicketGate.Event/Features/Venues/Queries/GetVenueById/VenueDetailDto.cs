namespace TicketGate.Event.Features.Venues.Queries.GetVenueById;

public sealed record VenueDetailDto(Guid Id, string Name, string Location, string SeatMap);
