using Mediator;
using TicketGate.Core.Pagination;
using TicketGate.Core.Results;

namespace TicketGate.Event.Features.Events.Queries.GetEventList;

public sealed record GetEventListQuery(
    int Page,
    int PageSize,
    string? Search,
    string? City,
    DateTime? StartsAfter) : IRequest<Result<PagedResult<EventListDto>>>;
