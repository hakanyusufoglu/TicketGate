using FluentValidation;

namespace TicketGate.Booking.Features.Tickets.Commands.ReserveTicket;

/// <summary>ReserveTicket komutu icin dogrulama kurallari.</summary>
internal sealed class ReserveTicketValidator : AbstractValidator<ReserveTicketCommand>
{
    /// <summary>
    /// TicketId ve UserId alanlarinin bos Guid olmamasini zorunlu kilar.
    /// Redis lock anahtari ve lock sahibi bilgisi bu iki deger uzerinden uretilir.
    /// </summary>
    public ReserveTicketValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
    }
}
