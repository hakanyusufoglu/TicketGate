using Mediator;
using TicketGate.Core.Domain;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.Tickets.Commands.GenerateTickets;

/// <summary>
/// Event icin ticket generate etme istegi.
/// SeatMap endpoint katmaninda Event modulunden okunup command'e eklenir; cross-module DB erisimi olmaz.
/// </summary>
public sealed record GenerateTicketsCommand(Guid EventId, SeatMap SeatMap)
    : IRequest<Result<GenerateTicketsResponse>>;
