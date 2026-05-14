using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using TicketGate.Event.Infrastructure.Persistence;

namespace TicketGate.Event.Features.Events.Commands.CreateEvent;

internal sealed class CreateEventHandler(EventDbContext db) : IRequestHandler<CreateEventCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var venueExists = await db.Venues
            .AnyAsync(venue => venue.Id == request.VenueId, cancellationToken);

        if (!venueExists)
        {
            return Result<Guid>.Fail(AppError.NotFound("Venue", request.VenueId));
        }

        var performerExists = await db.Performers
            .AnyAsync(performer => performer.Id == request.PerformerId, cancellationToken);

        if (!performerExists)
        {
            return Result<Guid>.Fail(AppError.NotFound("Performer", request.PerformerId));
        }

        var eventEntity = Domain.Entities.Event.Create(
            request.Name,
            request.Description,
            request.VenueId,
            request.PerformerId,
            request.StartsAt,
            request.EndsAt);

        await db.Events.AddAsync(eventEntity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(eventEntity.Id);
    }
}
