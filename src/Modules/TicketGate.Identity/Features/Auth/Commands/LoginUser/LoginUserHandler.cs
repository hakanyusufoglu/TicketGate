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
using TicketGate.Identity.Infrastructure.Persistence;
using RefreshTokenEntity = TicketGate.Identity.Domain.Entities.RefreshToken;

namespace TicketGate.Identity.Features.Auth.Commands.LoginUser;

public sealed class LoginUserHandler(IdentityDbContext db, IConfiguration config)
    : IRequestHandler<LoginUserCommand, Result<LoginUserResponse>>
{
    public async Task<Result<LoginUserResponse>> Handle(
        LoginUserCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(existingUser => existingUser.Email == email, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Result<LoginUserResponse>.Fail(AppError.Unauthorized("Invalid credentials."));
        }

        var tokenResult = CreateAccessToken(user, config);

        if (tokenResult.IsFailure)
        {
            return Result<LoginUserResponse>.Fail(tokenResult.Error!);
        }

        var refreshTokenValue = Guid.NewGuid().ToString("N");
        var refreshToken = RefreshTokenEntity.Create(user.Id, refreshTokenValue, DateTime.UtcNow.AddDays(7));

        await db.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result<LoginUserResponse>.Ok(new LoginUserResponse(
            tokenResult.Value!.AccessToken,
            refreshTokenValue,
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
