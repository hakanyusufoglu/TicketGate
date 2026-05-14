using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.Tickets.Queries.GetTicketById;

/// <summary>
/// Id ile tekil bilet sorgular. AsNoTracking ve Select projection kullanilir.
/// Entity tracking acilmadan yalnizca HTTP yaniti icin gereken kolonlar okunur.
/// </summary>
internal sealed class GetTicketByIdHandler(BookingDbContext db)
    : IRequestHandler<GetTicketByIdQuery, Result<TicketDetailDto>>
{
    /// <summary>
    /// Bilet detayini projection ile okur ve yoksa 404 doner.
    /// Query handler validator kullanmaz; bos Guid sonucu dogal olarak NotFound'a duser.
    /// </summary>
    public async Task<Result<TicketDetailDto>> Handle(
        GetTicketByIdQuery request,
        CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .AsNoTracking()
            .Where(item => item.Id == request.Id)
            .Select(item => new TicketDetailDto(
                item.Id,
                item.EventId,
                item.Seat,
                item.Price,
                item.Status.ToString(),
                item.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return ticket is null
            ? Result<TicketDetailDto>.Fail(AppError.NotFound("Ticket", request.Id))
            : Result<TicketDetailDto>.Ok(ticket);
    }
}
