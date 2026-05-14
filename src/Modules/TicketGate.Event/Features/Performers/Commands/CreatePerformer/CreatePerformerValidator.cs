using FluentValidation;

namespace TicketGate.Event.Features.Performers.Commands.CreatePerformer;

public sealed class CreatePerformerValidator : AbstractValidator<CreatePerformerCommand>
{
    public CreatePerformerValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Bio)
            .MaximumLength(2000);
    }
}
