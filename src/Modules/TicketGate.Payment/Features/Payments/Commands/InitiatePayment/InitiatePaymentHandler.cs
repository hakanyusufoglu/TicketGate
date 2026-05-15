using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Contracts;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using TicketGate.Payment.Infrastructure.Outbox;
using TicketGate.Payment.Infrastructure.Persistence;
using PaymentEntity = TicketGate.Payment.Domain.Entities.Payment;

namespace TicketGate.Payment.Features.Payments.Commands.InitiatePayment;

/// <summary>
/// Odeme baslatma komutunu isler. Payment ve OutboxMessage'i tek transaction'da atomik yazar.
/// Stripe veya PayPal bu handler'da cagrilmaz; idempotency key tekrar eden istekleri mevcut response ile yanitlar.
/// </summary>
internal sealed class InitiatePaymentHandler(
    PaymentDbContext db,
    ITicketReservationReader ticketReservationReader)
    : IRequestHandler<InitiatePaymentCommand, Result<InitiatePaymentResponse>>
{
    /// <summary>
    /// Idempotency kontrolu, reserved ticket dogrulamasi ve atomik Payment + Outbox yazimini yurutur.
    /// Harici gateway cagrisi yapilmaz; outbox mesaji worker tarafindan islenecek dayanirli kayittir.
    /// </summary>
    public async Task<Result<InitiatePaymentResponse>> Handle(
        InitiatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var existingPayment = await db.Payments
            .AsNoTracking()
            .Where(payment => payment.IdempotencyKey == request.IdempotencyKey)
            .Select(payment => new InitiatePaymentResponse(payment.Id, payment.Status.ToString()))
            .SingleOrDefaultAsync(cancellationToken);

        if (existingPayment is not null)
        {
            return Result<InitiatePaymentResponse>.Ok(existingPayment);
        }

        var ticketResult = await ticketReservationReader.GetReservedTicketAsync(request.TicketId, cancellationToken);
        if (ticketResult.IsFailure)
        {
            return Result<InitiatePaymentResponse>.Fail(ticketResult.Error!);
        }

        if (ticketResult.Value!.UserId != request.UserId)
        {
            return Result<InitiatePaymentResponse>.Fail(AppError.Conflict(
                "ticket.lock_owner_mismatch",
                $"Ticket '{request.TicketId}' is locked by another user."));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var payment = PaymentEntity.Create(
            request.TicketId,
            request.UserId,
            request.Amount,
            request.IdempotencyKey);

        var outboxMessage = OutboxMessage.Create(
            OutboxMessageTypes.PaymentInitiated,
            new PaymentInitiatedOutboxPayload(
                payment.Id,
                payment.TicketId,
                payment.UserId,
                payment.Amount,
                payment.Currency,
                request.Provider));

        await db.Payments.AddAsync(payment, cancellationToken);
        await db.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<InitiatePaymentResponse>.Ok(new InitiatePaymentResponse(
            payment.Id,
            payment.Status.ToString()));
    }
}
