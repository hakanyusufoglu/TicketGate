using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using TicketGate.Event.Infrastructure.Cache;
using TicketGate.Event.Infrastructure.Persistence;

namespace TicketGate.Event.Features.Events.Commands.PublishEvent;

/// <summary>
/// Event publish komutunu isler.
/// EF Core tracking ile state degistirir; basarili publish sonrasi detail cache ve event list output cache temizlenir.
/// </summary>
internal sealed class PublishEventHandler(
    EventDbContext db,
    IEventCacheService cacheService,
    IOutputCacheStore outputCacheStore) : IRequestHandler<PublishEventCommand, Result>
{
    /// <summary>
    /// Event'i tracked sorguyla yukler, publish state gecisini yapar ve cache invalidation uygular.
    /// Command handler tracking gerektirdigi icin AsNoTracking kullanmaz.
    /// </summary>
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
        await cacheService.InvalidateEventAsync(request.Id, cancellationToken);
        await outputCacheStore.EvictByTagAsync(EventCachePolicies.Events, cancellationToken);

        return Result.Ok();
    }
}
