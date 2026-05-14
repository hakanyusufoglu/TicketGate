using TicketGate.Core.Domain;

namespace TicketGate.Event.Features.Venues.Queries.GetVenueById;

/// <summary>Mekan detay yaniti. SeatMap typed value object olarak doner.</summary>
public sealed record VenueDetailDto(Guid Id, string Name, string Location, SeatMap SeatMap);
