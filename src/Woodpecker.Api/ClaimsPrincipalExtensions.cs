using System.Security.Claims;

namespace Woodpecker.Api;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Le token ne contient pas d'identifiant utilisateur.");

        return Guid.Parse(value);
    }
}
