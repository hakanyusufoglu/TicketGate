using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using TicketGate.Payment.Domain.Enums;
using TicketGate.Payment.Infrastructure.Outbox;
using TicketGate.Payment.Infrastructure.Persistence;

namespace TicketGate.Payment.Features.Payments.Commands.RefundPayment;

/// <summary>
/// Iade talebini isler. Payment Completed olmalidir ve harici gateway bu handler'da cagrilmaz.
/// Refund istegi OutboxMessage olarak yazilir; fiili iade OutboxWorker tarafindan tamamlanir.
/// </summary>
internal sealed class RefundPaymentHandler(PaymentDbContext db)
    : IRequestHandler<RefundPaymentCommand, Result>
{
    /// <summary>
    /// Payment sahipligi ve Completed durumunu dogrular, refund talebini outbox'a yazar.
    /// Status hemen Refunded yapilmaz; harici gateway sonucu worker tarafindan dogrulanmalidir.
    /// </summary>
    public async Task<Result> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await db.Payments.SingleOrDefaultAsync(
            item => item.Id == request.PaymentId,
            cancellationToken);

        if (payment is null)
        {
            return Result.Fail(AppError.NotFound("Payment", request.PaymentId));
        }

        if (payment.UserId != request.UserId)
        {
            return Result.Fail(AppError.Conflict(
                "payment.user_mismatch",
                $"Payment '{request.PaymentId}' belongs to another user."));
        }

        if (payment.Status != PaymentStatus.Completed || string.IsNullOrWhiteSpace(payment.ExternalPaymentId))
        {
            return Result.Fail(AppError.Conflict(
                "payment.not_refundable",
                $"Payment '{request.PaymentId}' is not refundable."));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var outbox = OutboxMessage.Create(
            OutboxMessageTypes.PaymentRefundRequested,
            new RefundPaymentOutboxPayload(
                payment.Id,
                payment.TicketId,
                payment.UserId,
                payment.ExternalPaymentId));

        await db.OutboxMessages.AddAsync(outbox, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Ok();
    }
}
