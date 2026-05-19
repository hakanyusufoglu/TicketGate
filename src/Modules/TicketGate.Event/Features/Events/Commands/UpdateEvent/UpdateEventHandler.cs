using Mediator;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using TicketGate.Event.Infrastructure.Cache;
using TicketGate.Event.Infrastructure.Persistence;

namespace TicketGate.Event.Features.Events.Commands.UpdateEvent;

/// <summary>
/// Draft event guncelleme komutunu isler.
/// EF Core tracking ile entity state degisir ve basarili kayit sonrasi event detail cache'i temizlenir.
/// </summary>
public sealed class UpdateEventHandler(EventDbContext db, IEventCacheService cacheService)
    : IRequestHandler<UpdateEventCommand, Result>
{
    /// <summary>
    /// Event'i tracked sorguyla yukler, published kontrolunu yapar ve guncelleme sonrasi stale cache kaydini siler.
    /// Command handler tracking gerektirdigi icin AsNoTracking kullanmaz.
    /// </summary>
    public async ValueTask<Result> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
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
        await cacheService.InvalidateEventAsync(request.Id, cancellationToken);

        return Result.Ok();
    }
}
