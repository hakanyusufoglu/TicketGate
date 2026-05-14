using FluentValidation;

namespace TicketGate.Event.Features.Events.Commands.PublishEvent;

public sealed class PublishEventValidator : AbstractValidator<PublishEventCommand>
{
    public PublishEventValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
