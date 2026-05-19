using Mediator;
using TicketGate.Core.Results;

namespace TicketGate.Identity.Features.Auth.Commands.RegisterUser;

public sealed record RegisterUserCommand(string Email, string Password, string FullName)
    : IRequest<Result<RegisterUserResponse>>;
