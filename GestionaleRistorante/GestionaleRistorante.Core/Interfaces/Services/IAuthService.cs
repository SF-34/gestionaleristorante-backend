using GestionaleRistorante.Core.Models;

namespace GestionaleRistorante.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthTokensResult> SignupAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default);

    Task<AuthTokensResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<AuthTokensResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(
        int userId,
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task ChangePasswordFirstLoginAsync(
        int userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
}
