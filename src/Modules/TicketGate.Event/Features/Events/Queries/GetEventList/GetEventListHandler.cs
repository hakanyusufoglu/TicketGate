using Mediator;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Pagination;
using TicketGate.Core.Results;
using TicketGate.Event.Infrastructure.Persistence;

namespace TicketGate.Event.Features.Events.Queries.GetEventList;

public sealed class GetEventListHandler(EventDbContext db)
    : IRequestHandler<GetEventListQuery, Result<PagedResult<EventListDto>>>
{
    public async ValueTask<Result<PagedResult<EventListDto>>> Handle(
        GetEventListQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);
        var isPostgres = IsPostgresProvider();
        var query =
            from eventEntity in db.Events.AsNoTracking()
            join venue in db.Venues.AsNoTracking() on eventEntity.VenueId equals venue.Id
            join performer in db.Performers.AsNoTracking() on eventEntity.PerformerId equals performer.Id
            where eventEntity.IsPublished
            select new
            {
                Event = eventEntity,
                Venue = venue,
                Performer = performer
            };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = isPostgres
                ? query.Where(row => EF.Functions.ILike(row.Event.Name, ToLikePattern(search), "\\"))
                : query.Where(row => row.Event.Name.ToLowerInvariant().Contains(search.ToLowerInvariant()));
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City.Trim();
            query = isPostgres
                ? query.Where(row => EF.Functions.ILike(row.Venue.Location, ToLikePattern(city), "\\"))
                : query.Where(row => row.Venue.Location.ToLowerInvariant().Contains(city.ToLowerInvariant()));
        }

        if (request.StartsAfter is not null)
        {
            query = query.Where(row => row.Event.StartsAt >= request.StartsAfter.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(row => row.Event.StartsAt)
            .ThenBy(row => row.Event.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(row => new EventListDto(
                row.Event.Id,
                row.Event.Name,
                row.Venue.Name,
                row.Venue.Location,
                row.Performer.Name,
                row.Event.StartsAt,
                row.Event.IsPublished))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<EventListDto>>.Ok(
            new PagedResult<EventListDto>(items, totalCount, page, pageSize));
    }

    private bool IsPostgresProvider()
    {
        return db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string ToLikePattern(string value)
    {
        var escaped = value
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");

        return $"%{escaped}%";
    }
}
