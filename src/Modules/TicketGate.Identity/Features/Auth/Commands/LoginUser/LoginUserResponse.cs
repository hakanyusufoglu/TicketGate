namespace TicketGate.Identity.Features.Auth.Commands.LoginUser;

public sealed record LoginUserResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
