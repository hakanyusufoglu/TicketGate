using MediatR;
using TicketGate.Core.Results;
using TicketGate.Event.Domain.Entities;
using TicketGate.Event.Infrastructure.Persistence;

namespace TicketGate.Event.Features.Venues.Commands.CreateVenue;

/// <summary>
/// CreateVenue komutunu isler. Venue aggregate'i typed SeatMap ile olusturulur;
/// string JSON parse etme sorumlulugu domain veya handler icine dagitilmaz.
/// </summary>
internal sealed class CreateVenueHandler(EventDbContext db) : IRequestHandler<CreateVenueCommand, Result<Guid>>
{
    /// <summary>
    /// Mekan bilgisini Event schema'sina kaydeder.
    /// CancellationToken EF Core async cagrisina tasinir.
    /// </summary>
    public async Task<Result<Guid>> Handle(CreateVenueCommand request, CancellationToken cancellationToken)
    {
        var venue = Venue.Create(request.Name, request.Location, request.SeatMap);

        await db.Venues.AddAsync(venue, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(venue.Id);
    }
}
