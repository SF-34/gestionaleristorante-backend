using System.Security.Claims;
using GestionaleRistorante.Common;
using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Interfaces.Repositories;

namespace GestionaleRistorante.Middleware;

public sealed class RequirePasswordChangeMiddleware(
    RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IRepository<User> userRepository)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (IsExcludedPath(path))
        {
            await next(context);
            return;
        }

        var userIdClaim = context.User.FindFirstValue("sub")
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            await next(context);
            return;
        }

        var user = await userRepository.GetByIdAsync(userId, context.RequestAborted);
        if (user is null || !user.RequirePasswordChange)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";

        var payload = ApiResponse<object?>.Failure(
            "AUTH_PASSWORD_CHANGE_REQUIRED",
            "Password change is required before accessing protected resources.",
            StatusCodes.Status403Forbidden);

        await context.Response.WriteAsJsonAsync(payload, context.RequestAborted);
    }

    private static bool IsExcludedPath(string path)
    {
        return path.StartsWith("/api/v1/auth/login", StringComparison.Ordinal)
            || path.StartsWith("/api/v1/auth/signup", StringComparison.Ordinal)
            || path.StartsWith("/api/v1/auth/refresh", StringComparison.Ordinal)
            || path.StartsWith("/api/v1/auth/logout", StringComparison.Ordinal)
            || path.StartsWith("/api/v1/auth/change-password-first-login", StringComparison.Ordinal)
            || path.StartsWith("/api/v1/dishes", StringComparison.Ordinal);
    }
}
