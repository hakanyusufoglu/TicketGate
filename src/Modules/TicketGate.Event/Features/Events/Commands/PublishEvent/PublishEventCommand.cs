using Mediator;
using TicketGate.Core.Results;

namespace TicketGate.Event.Features.Events.Commands.PublishEvent;

public sealed record PublishEventCommand(Guid Id) : IRequest<Result>;
