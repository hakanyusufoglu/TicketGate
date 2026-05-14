using MediatR;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Features.Tickets.Queries.GetAvailableSeats;

/// <summary>Etkinlige ait musait koltuklari listeler.</summary>
public sealed record GetAvailableSeatsQuery(Guid EventId) : IRequest<Result<List<SeatDto>>>;
