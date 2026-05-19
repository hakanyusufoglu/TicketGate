using Mediator;
using TicketGate.Core.Results;

namespace TicketGate.Payment.Features.Payments.Commands.RefundPayment;

/// <summary>Iade talebi istegi.</summary>
public sealed record RefundPaymentCommand(Guid PaymentId, Guid UserId) : IRequest<Result>;
