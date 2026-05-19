using Mediator;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using TicketGate.Event.Infrastructure.Cache;
using TicketGate.Event.Infrastructure.Persistence;

namespace TicketGate.Event.Features.Events.Queries.GetEventById;

/// <summary>
/// Event detay sorgusunu cache-aside pattern ile yurutur.
/// Once Redis cache okunur; cache miss durumunda projection-first EF Core sorgusu calisir.
/// </summary>
public sealed class GetEventByIdHandler(EventDbContext db, IEventCacheService cacheService)
    : IRequestHandler<GetEventByIdQuery, Result<EventDetailDto>>
{
    /// <summary>
    /// Event detayini Redis cache veya Postgres projection sorgusundan dondurur.
    /// Query handler AsNoTracking kullanir ve cache hatalarinda DB fallback davranisini korur.
    /// </summary>
    public async ValueTask<Result<EventDetailDto>> Handle(
        GetEventByIdQuery request,
        CancellationToken cancellationToken)
    {
        var cachedEvent = await cacheService.GetEventAsync(request.Id, cancellationToken);
        if (cachedEvent is not null)
        {
            return Result<EventDetailDto>.Ok(cachedEvent);
        }

        var eventDetail = await db.Events
            .AsNoTracking()
            .Where(eventEntity => eventEntity.Id == request.Id)
            .Select(eventEntity => new EventDetailDto(
                eventEntity.Id,
                eventEntity.Name,
                eventEntity.Description,
                eventEntity.Venue.Name,
                eventEntity.Venue.Location,
                eventEntity.Venue.SeatMap,
                eventEntity.Performer.Name,
                eventEntity.Performer.Bio,
                eventEntity.StartsAt,
                eventEntity.EndsAt,
                eventEntity.IsPublished))
            .FirstOrDefaultAsync(cancellationToken);

        if (eventDetail is null)
        {
            return Result<EventDetailDto>.Fail(AppError.NotFound("Event", request.Id));
        }

        await cacheService.SetEventAsync(request.Id, eventDetail, cancellationToken);

        return Result<EventDetailDto>.Ok(eventDetail);
    }
}
