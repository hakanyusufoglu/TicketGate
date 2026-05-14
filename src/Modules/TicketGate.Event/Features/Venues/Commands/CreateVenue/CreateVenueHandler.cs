using MediatR;
using TicketGate.Core.Results;
using TicketGate.Event.Domain.Entities;
using TicketGate.Event.Infrastructure.Persistence;

namespace TicketGate.Event.Features.Venues.Commands.CreateVenue;

internal sealed class CreateVenueHandler(EventDbContext db) : IRequestHandler<CreateVenueCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateVenueCommand request, CancellationToken cancellationToken)
    {
        var venue = Venue.Create(request.Name, request.Location, request.SeatMap);

        await db.Venues.AddAsync(venue, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(venue.Id);
    }
}
