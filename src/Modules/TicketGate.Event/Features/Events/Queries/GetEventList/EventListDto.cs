namespace TicketGate.Event.Features.Events.Queries.GetEventList;

public sealed record EventListDto(
    Guid Id,
    string Name,
    string VenueName,
    string VenueLocation,
    string PerformerName,
    DateTime StartsAt,
    bool IsPublished);
