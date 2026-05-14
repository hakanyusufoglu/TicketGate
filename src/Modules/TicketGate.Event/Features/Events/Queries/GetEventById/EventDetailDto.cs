using TicketGate.Core.Domain;

namespace TicketGate.Event.Features.Events.Queries.GetEventById;

/// <summary>Event detay yaniti. Venue ve performer ozetleriyle birlikte SeatMap bilgisini tasir.</summary>
public sealed record EventDetailDto(
    Guid Id,
    string Name,
    string Description,
    string VenueName,
    string VenueLocation,
    SeatMap SeatMap,
    string PerformerName,
    string PerformerBio,
    DateTime StartsAt,
    DateTime EndsAt,
    bool IsPublished);
