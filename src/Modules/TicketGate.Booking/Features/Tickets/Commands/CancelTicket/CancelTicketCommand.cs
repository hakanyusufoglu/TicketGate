using MediatR;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.Tickets.Commands.CancelTicket;

/// <summary>Bilet iptal istegi. Yalnizca Confirmed biletler iptal edilebilir.</summary>
public sealed record CancelTicketCommand(Guid TicketId, Guid UserId) : IRequest<Result>;
