using Mediator;
using TicketGate.Core.Results;
using TicketGate.Identity.Features.Auth.Commands.LoginUser;

namespace TicketGate.Identity.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken)
    : IRequest<Result<LoginUserResponse>>;
