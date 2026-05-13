using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using TicketGate.Identity.Domain.Entities;
using TicketGate.Identity.Features.Auth.Commands.LoginUser;
using TicketGate.Identity.Infrastructure.Persistence;
using RefreshTokenEntity = TicketGate.Identity.Domain.Entities.RefreshToken;

namespace TicketGate.Identity.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenHandler(IdentityDbContext db, IConfiguration config)
    : IRequestHandler<RefreshTokenCommand, Result<LoginUserResponse>>
{
    public async Task<Result<LoginUserResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var refreshToken = await db.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.Token == request.RefreshToken, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
        {
            return Result<LoginUserResponse>.Fail(AppError.Unauthorized("Invalid or expired token."));
        }

        var tokenResult = CreateAccessToken(refreshToken.User, config);

        if (tokenResult.IsFailure)
        {
            return Result<LoginUserResponse>.Fail(tokenResult.Error!);
        }

        refreshToken.Revoke();

        var newRefreshTokenValue = Guid.NewGuid().ToString("N");
        var newRefreshToken = RefreshTokenEntity.Create(
            refreshToken.UserId,
            newRefreshTokenValue,
            DateTime.UtcNow.AddDays(7));

        await db.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result<LoginUserResponse>.Ok(new LoginUserResponse(
            tokenResult.Value!.AccessToken,
            newRefreshTokenValue,
            tokenResult.Value.ExpiresAt));
    }

    private static Result<AccessTokenResult> CreateAccessToken(User user, IConfiguration config)
    {
        var issuer = config["Jwt:Issuer"];
        var audience = config["Jwt:Audience"];
        var secretKey = config["Jwt:SecretKey"];

        if (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(secretKey) ||
            Encoding.UTF8.GetByteCount(secretKey) < 32)
        {
            return Result<AccessTokenResult>.Fail(new AppError(
                AppErrorType.Internal,
                "Jwt.InvalidConfiguration",
                "JWT configuration is invalid."));
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("full_name", user.FullName)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return Result<AccessTokenResult>.Ok(new AccessTokenResult(accessToken, expiresAt));
    }

    private sealed record AccessTokenResult(string AccessToken, DateTime ExpiresAt);
}
