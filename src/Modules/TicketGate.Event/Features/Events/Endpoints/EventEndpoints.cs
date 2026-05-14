using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TicketGate.Core.Extensions;
using TicketGate.Event.Features.Events.Commands.CreateEvent;
using TicketGate.Event.Features.Events.Commands.PublishEvent;
using TicketGate.Event.Features.Events.Commands.UpdateEvent;
using TicketGate.Event.Features.Events.Queries.GetEventById;
using TicketGate.Event.Features.Events.Queries.GetEventList;
using TicketGate.Event.Features.Performers.Commands.CreatePerformer;
using TicketGate.Event.Features.Venues.Commands.CreateVenue;
using TicketGate.Event.Features.Venues.Queries.GetVenueById;

namespace TicketGate.Event.Features.Events.Endpoints;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Events");

        group.MapPost("/events", async (
            CreateEventCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(StatusCodes.Status201Created);
        });

        group.MapPut("/events/{id:guid}", async (
            Guid id,
            UpdateEventRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new UpdateEventCommand(id, request.Name, request.Description, request.StartsAt, request.EndsAt),
                cancellationToken);

            return result.ToHttpResult(StatusCodes.Status204NoContent);
        });

        group.MapPost("/events/{id:guid}/publish", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new PublishEventCommand(id), cancellationToken);
            return result.ToHttpResult(StatusCodes.Status204NoContent);
        });

        group.MapGet("/events/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetEventByIdQuery(id), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapGet("/events", async (
            ISender sender,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20,
            string? search = null,
            string? city = null,
            DateTime? startsAfter = null) =>
        {
            var result = await sender.Send(
                new GetEventListQuery(page, pageSize, search, city, startsAfter),
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapPost("/venues", async (
            CreateVenueCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(StatusCodes.Status201Created);
        });

        group.MapGet("/venues/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetVenueByIdQuery(id), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPost("/performers", async (
            CreatePerformerCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(StatusCodes.Status201Created);
        });

        return app;
    }

    private sealed record UpdateEventRequest(
        string Name,
        string Description,
        DateTime StartsAt,
        DateTime EndsAt);
}
