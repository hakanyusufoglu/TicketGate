using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using TicketGate.Event.Infrastructure.Persistence;

namespace TicketGate.Event.Features.Events.Queries.GetEventById;

internal sealed class GetEventByIdHandler(EventDbContext db)
    : IRequestHandler<GetEventByIdQuery, Result<EventDetailDto>>
{
    public async Task<Result<EventDetailDto>> Handle(
        GetEventByIdQuery request,
        CancellationToken cancellationToken)
    {
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

        return Result<EventDetailDto>.Ok(eventDetail);
    }
}
