using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Enums;
using GestionaleRistorante.Core.Exceptions;
using GestionaleRistorante.Core.Interfaces.Repositories;
using GestionaleRistorante.Core.Interfaces.Services;
using GestionaleRistorante.Core.Models;
using GestionaleRistorante.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GestionaleRistorante.Infrastructure.Services;

public sealed class AuthService(
    IRepository<User> userRepository,
    IRepository<RefreshToken> refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasherService passwordHasherService,
    IJwtTokenService jwtTokenService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthTokensResult> SignupAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var emailExists = await userRepository.Query().AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (emailExists)
        {
            throw new AppException("AUTH_EMAIL_ALREADY_EXISTS", "Email already registered.", 409);
        }

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHasherService.HashPassword(password),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            RoleId = (int)RoleIds.Customer,
            RequirePasswordChange = false
        };

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = CreateTokenPair(user, out var refreshTokenEntity);
        await refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<AuthTokensResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await userRepository.Query().FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null || !passwordHasherService.VerifyPassword(user.PasswordHash, password))
        {
            throw new AppException("AUTH_INVALID_CREDENTIALS", "Invalid credentials.", 401);
        }

        var result = CreateTokenPair(user, out var refreshTokenEntity);
        await refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<AuthTokensResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var refreshTokenHash = jwtTokenService.HashRefreshToken(refreshToken);
        var existingToken = await refreshTokenRepository.Query()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == refreshTokenHash, cancellationToken);

        if (existingToken is null
            || existingToken.IsRevoked
            || existingToken.ExpiresAtUtc <= DateTime.UtcNow
            || existingToken.User.IsDeleted)
        {
            throw new AppException("AUTH_REFRESH_TOKEN_INVALID", "Refresh token is invalid or expired.", 401);
        }

        existingToken.IsRevoked = true;
        existingToken.RevokedAtUtc = DateTime.UtcNow;

        var result = CreateTokenPair(existingToken.User, out var newRefreshTokenEntity);
        existingToken.ReplacedByTokenHash = newRefreshTokenEntity.TokenHash;

        refreshTokenRepository.Update(existingToken);
        await refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task LogoutAsync(int userId, string refreshToken, CancellationToken cancellationToken = default)
    {
        var refreshTokenHash = jwtTokenService.HashRefreshToken(refreshToken);
        var existingToken = await refreshTokenRepository.Query()
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.TokenHash == refreshTokenHash && !x.IsRevoked,
                cancellationToken);

        if (existingToken is null)
        {
            return;
        }

        existingToken.IsRevoked = true;
        existingToken.RevokedAtUtc = DateTime.UtcNow;
        refreshTokenRepository.Update(existingToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordFirstLoginAsync(
        int userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new AppException("AUTH_USER_NOT_FOUND", "User not found.", 404);
        }

        if (!user.RequirePasswordChange)
        {
            throw new AppException("AUTH_PASSWORD_CHANGE_NOT_REQUIRED", "Password change is not required.", 400);
        }

        if (!passwordHasherService.VerifyPassword(user.PasswordHash, currentPassword))
        {
            throw new AppException("AUTH_INVALID_CREDENTIALS", "Current password is invalid.", 401);
        }

        user.PasswordHash = passwordHasherService.HashPassword(newPassword);
        user.RequirePasswordChange = false;
        userRepository.Update(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private AuthTokensResult CreateTokenPair(User user, out RefreshToken refreshTokenEntity)
    {
        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var refreshTokenData = jwtTokenService.GenerateRefreshToken();

        refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenData.TokenHash,
            ExpiresAtUtc = refreshTokenData.ExpiresAtUtc
        };

        return new AuthTokensResult(
            accessToken,
            refreshTokenData.PlainToken,
            DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes),
            user.Id,
            user.RoleId,
            user.RequirePasswordChange);
    }
}
