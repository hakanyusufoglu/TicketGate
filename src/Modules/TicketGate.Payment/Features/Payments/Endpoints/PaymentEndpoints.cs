using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TicketGate.Core.Extensions;
using TicketGate.Core.Security;
using TicketGate.Payment.Features.Payments.Commands.InitiatePayment;
using TicketGate.Payment.Features.Payments.Commands.RefundPayment;
using TicketGate.Payment.Features.Payments.Queries.GetPaymentById;

namespace TicketGate.Payment.Features.Payments.Endpoints;

/// <summary>
/// Odeme islemleri HTTP endpoint'leri.
/// Tum is mantigi handler'larda; endpoint sadece HTTP request'i command/query nesnesine cevirir.
/// </summary>
public static class PaymentEndpoints
{
    /// <summary>
    /// Payment endpoint'lerini /api/v1/payments grubu altinda kaydeder.
    /// Basarili initiate 201, refund 204, query 200 donuslerini Result extension uzerinden uretir.
    /// </summary>
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/payments").WithTags("Payments");

        group.MapPost("/initiate", async (
            InitiatePaymentCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(StatusCodes.Status201Created);
        })
            .WithName("InitiatePayment")
            .WithSummary("Odeme baslatir")
            .WithDescription("""
                Ticket icin odeme surecini baslatir.
                UserId JWT token'dan okunur; body'den alinmaz.
                Amount ticket fiyatindan hesaplanir; client manipulasyonu engellenir.
                IdempotencyKey ile network retry'da cifte odeme engellenir.
                Stripe veya PayPal direkt cagrilmaz; OutboxWorker ustlenir.
                """)
            .Produces<InitiatePaymentResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Reserve);

        group.MapPost("/{id:guid}/refund", async (
            Guid id,
            ISender sender,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var userId = context.GetUserId();
            var result = await sender.Send(new RefundPaymentCommand(id, userId), cancellationToken);
            return result.ToHttpResult(StatusCodes.Status204NoContent);
        })
            .WithName("RefundPayment")
            .WithSummary("Odeme iadesi baslatir")
            .WithDescription("""
                Tamamlanmis odeme icin iade surecini outbox uzerinden baslatir.
                UserId JWT token'dan okunur; body'den alinmaz.
                Harici gateway endpoint icinde cagrilmaz; OutboxWorker iade mesajini isler.
                """)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Reserve);

        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetPaymentByIdQuery(id), cancellationToken);
            return result.ToHttpResult();
        })
            .WithName("GetPaymentById")
            .WithSummary("Odeme detayini getirir")
            .WithDescription("""
                Id ile odeme detay bilgisini projection-first okur.
                Query handler entity tracking olusturmaz.
                """)
            .Produces<PaymentDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        return app;
    }
}
