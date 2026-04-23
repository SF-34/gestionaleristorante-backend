using System.Security.Claims;
using GestionaleRistorante.Core.Exceptions;

namespace GestionaleRistorante.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (!int.TryParse(subject, out var userId))
        {
            throw new AppException("AUTH_INVALID_TOKEN", "Invalid token subject.", 401);
        }

        return userId;
    }

    public static int GetRequiredRoleId(this ClaimsPrincipal principal)
    {
        var roleIdText = principal.FindFirstValue("roleId")
            ?? principal.FindFirstValue(ClaimTypes.Role);

        if (!int.TryParse(roleIdText, out var roleId))
        {
            throw new AppException("AUTH_INVALID_TOKEN", "Invalid token role.", 401);
        }

        return roleId;
    }
}
