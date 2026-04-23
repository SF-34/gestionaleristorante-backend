namespace GestionaleRistorante.Core.Models;

public sealed record AuthTokensResult(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    int UserId,
    int RoleId,
    bool RequirePasswordChange);
