using MediatR;
using TicketGate.Core.Results;

namespace TicketGate.Event.Features.Performers.Commands.CreatePerformer;

public sealed record CreatePerformerCommand(string Name, string Bio) : IRequest<Result<Guid>>;
