using System.Security.Claims;

namespace Fortuno.DTO.Common;

public static class UserContextExtensions
{
    public static long GetCurrentUserId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("userId")
            ?? principal.FindFirst(ClaimTypes.NameIdentifier);
        if (claim is null || !long.TryParse(claim.Value, out var id))
            throw new UnauthorizedAccessException("Usuário não autenticado.");
        return id;
    }

    public static long? TryGetCurrentUserId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("userId") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && long.TryParse(claim.Value, out var id) ? id : null;
    }
}
