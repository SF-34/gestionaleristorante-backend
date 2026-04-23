using GestionaleRistorante.Common.Extensions;
using GestionaleRistorante.Contracts;
using GestionaleRistorante.Core.Interfaces.Services;
using GestionaleRistorante.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionaleRistorante.Controllers;

[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("signup")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.SignupAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            cancellationToken);

        return Success(ToResponse(result), StatusCodes.Status201Created);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
        return Success(ToResponse(result));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RefreshAsync(request.RefreshToken, cancellationToken);
        return Success(ToResponse(result));
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        await authService.LogoutAsync(userId, request.RefreshToken, cancellationToken);
        return SuccessNoData();
    }

    [Authorize]
    [HttpPost("change-password-first-login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePasswordFirstLogin(
        [FromBody] ChangePasswordFirstLoginRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        await authService.ChangePasswordFirstLoginAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        return SuccessNoData();
    }

    private static AuthResponse ToResponse(AuthTokensResult result)
    {
        return new AuthResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            AccessTokenExpiresAtUtc = result.AccessTokenExpiresAtUtc,
            UserId = result.UserId,
            RoleId = result.RoleId,
            RequirePasswordChange = result.RequirePasswordChange
        };
    }
}
