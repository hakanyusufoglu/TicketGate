using MediatR;
using TicketGate.Core.Results;

namespace TicketGate.Identity.Features.Auth.Commands.LoginUser;

public sealed record LoginUserCommand(string Email, string Password)
    : IRequest<Result<LoginUserResponse>>;
