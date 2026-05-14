using MediatR;
using TicketGate.Core.Results;
using TicketGate.Event.Domain.Entities;
using TicketGate.Event.Infrastructure.Persistence;

namespace TicketGate.Event.Features.Performers.Commands.CreatePerformer;

internal sealed class CreatePerformerHandler(EventDbContext db)
    : IRequestHandler<CreatePerformerCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreatePerformerCommand request,
        CancellationToken cancellationToken)
    {
        var performer = Performer.Create(request.Name, request.Bio);

        await db.Performers.AddAsync(performer, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(performer.Id);
    }
}
