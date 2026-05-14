using MediatR;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.Tickets.Commands.ConfirmTicket;

/// <summary>Odeme basarili oldugunda bileti onaylama istegi.</summary>
public sealed record ConfirmTicketCommand(Guid TicketId, Guid UserId) : IRequest<Result>;
