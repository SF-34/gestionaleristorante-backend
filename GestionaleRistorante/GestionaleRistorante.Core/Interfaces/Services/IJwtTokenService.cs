using GestionaleRistorante.Core.Entities;

namespace GestionaleRistorante.Core.Interfaces.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);

    (string PlainToken, string TokenHash, DateTime ExpiresAtUtc) GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);
}
