using FluentValidation;

namespace TicketGate.Payment.Features.Payments.Commands.InitiatePayment;

/// <summary>InitiatePayment komutu dogrulama kurallari.</summary>
internal sealed class InitiatePaymentValidator : AbstractValidator<InitiatePaymentCommand>
{
    /// <summary>
    /// Ticket, kullanici, tutar, provider ve idempotency key zorunluluklarini tanimlar.
    /// Provider degeri desteklenen gateway isimleriyle sinirlanir.
    /// </summary>
    public InitiatePaymentValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Provider)
            .NotEmpty()
            .Must(provider => provider is "Stripe" or "PayPal");
        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(100);
    }
}
