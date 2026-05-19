using Mediator;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.Tickets.Commands.ReserveTicket;

/// <summary>Bilet rezervasyon istegi. TicketId ve UserId zorunludur.</summary>
public sealed record ReserveTicketCommand(Guid TicketId, Guid UserId)
    : IRequest<Result<ReserveTicketResponse>>;
