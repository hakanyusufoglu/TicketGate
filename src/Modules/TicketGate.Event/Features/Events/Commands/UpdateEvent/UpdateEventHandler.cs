using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using TicketGate.Event.Infrastructure.Persistence;

namespace TicketGate.Event.Features.Events.Commands.UpdateEvent;

internal sealed class UpdateEventHandler(EventDbContext db) : IRequestHandler<UpdateEventCommand, Result>
{
    public async Task<Result> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var eventEntity = await db.Events
            .FirstOrDefaultAsync(existingEvent => existingEvent.Id == request.Id, cancellationToken);

        if (eventEntity is null)
        {
            return Result.Fail(AppError.NotFound("Event", request.Id));
        }

        if (eventEntity.IsPublished)
        {
            return Result.Fail(AppError.Conflict(
                "Event.AlreadyPublished",
                "Published events cannot be updated."));
        }

        eventEntity.Update(request.Name, request.Description, request.StartsAt, request.EndsAt);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
