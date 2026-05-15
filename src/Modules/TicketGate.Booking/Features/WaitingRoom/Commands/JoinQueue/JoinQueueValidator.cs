using FluentValidation;

namespace TicketGate.Booking.Features.WaitingRoom.Commands.JoinQueue;

/// <summary>JoinQueue komutu dogrulama kurallari.</summary>
internal sealed class JoinQueueValidator : AbstractValidator<JoinQueueCommand>
{
    /// <summary>EventId ve UserId alanlarinin bos olmamasini garanti eder.</summary>
    public JoinQueueValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
    }
}
