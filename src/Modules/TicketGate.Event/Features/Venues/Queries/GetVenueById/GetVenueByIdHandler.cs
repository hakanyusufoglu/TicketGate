using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using TicketGate.Event.Infrastructure.Persistence;

namespace TicketGate.Event.Features.Venues.Queries.GetVenueById;

internal sealed class GetVenueByIdHandler(EventDbContext db)
    : IRequestHandler<GetVenueByIdQuery, Result<VenueDetailDto>>
{
    public async Task<Result<VenueDetailDto>> Handle(
        GetVenueByIdQuery request,
        CancellationToken cancellationToken)
    {
        var venue = await db.Venues
            .AsNoTracking()
            .Where(existingVenue => existingVenue.Id == request.Id)
            .Select(existingVenue => new VenueDetailDto(
                existingVenue.Id,
                existingVenue.Name,
                existingVenue.Location,
                existingVenue.SeatMap))
            .FirstOrDefaultAsync(cancellationToken);

        if (venue is null)
        {
            return Result<VenueDetailDto>.Fail(AppError.NotFound("Venue", request.Id));
        }

        return Result<VenueDetailDto>.Ok(venue);
    }
}
