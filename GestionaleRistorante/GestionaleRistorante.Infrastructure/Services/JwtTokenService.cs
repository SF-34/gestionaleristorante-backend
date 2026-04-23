using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Interfaces.Services;
using GestionaleRistorante.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GestionaleRistorante.Infrastructure.Services;

public sealed class JwtTokenService(IOptions<JwtOptions> jwtOptions) : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public string GenerateAccessToken(User user)
    {
        var accessSecret = Encoding.UTF8.GetBytes(_jwtOptions.AccessTokenSecret);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("roleId", user.RoleId.ToString()),
            new(ClaimTypes.Role, user.RoleId.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(accessSecret),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(securityToken);
    }

    public (string PlainToken, string TokenHash, DateTime ExpiresAtUtc) GenerateRefreshToken()
    {
        var plainToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenHash = HashRefreshToken(plainToken);
        var expiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);
        return (plainToken, tokenHash, expiresAtUtc);
    }

    public string HashRefreshToken(string refreshToken)
    {
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
