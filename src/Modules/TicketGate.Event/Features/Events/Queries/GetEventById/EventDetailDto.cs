namespace TicketGate.Event.Features.Events.Queries.GetEventById;

public sealed record EventDetailDto(
    Guid Id,
    string Name,
    string Description,
    string VenueName,
    string VenueLocation,
    string SeatMap,
    string PerformerName,
    string PerformerBio,
    DateTime StartsAt,
    DateTime EndsAt,
    bool IsPublished);
