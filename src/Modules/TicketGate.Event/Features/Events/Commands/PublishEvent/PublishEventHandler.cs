using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using TicketGate.Event.Infrastructure.Persistence;

namespace TicketGate.Event.Features.Events.Commands.PublishEvent;

internal sealed class PublishEventHandler(EventDbContext db) : IRequestHandler<PublishEventCommand, Result>
{
    public async Task<Result> Handle(PublishEventCommand request, CancellationToken cancellationToken)
    {
        var eventEntity = await db.Events
            .FirstOrDefaultAsync(existingEvent => existingEvent.Id == request.Id, cancellationToken);

        if (eventEntity is null)
        {
            return Result.Fail(AppError.NotFound("Event", request.Id));
        }

        if (!eventEntity.Publish())
        {
            return Result.Fail(AppError.Conflict(
                "Event.AlreadyPublished",
                "Event is already published."));
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
