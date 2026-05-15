using MediatR;
using TicketGate.Core.Results;

namespace TicketGate.Payment.Features.Payments.Commands.InitiatePayment;

/// <summary>Odeme baslatma istegi. UserId JWT'den, Amount ticket fiyatindan okunur; IdempotencyKey retry korumasi saglar.</summary>
public sealed record InitiatePaymentCommand(
    Guid TicketId,
    string Provider,
    string IdempotencyKey) : IRequest<Result<InitiatePaymentResponse>>;
