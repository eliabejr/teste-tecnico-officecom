using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BCBGames.API.Auth;

public static class UserClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var candidates = new[]
        {
            user.FindFirstValue(JwtRegisteredClaimNames.Sub),
            user.FindFirstValue(ClaimTypes.NameIdentifier),
            user.FindFirstValue("sub"),
            user.FindFirstValue("userId"),
        };

        foreach (var value in candidates)
        {
            if (Guid.TryParse(value, out var id))
                return id;
        }

        throw new UnauthorizedAccessException("Invalid token: missing or invalid user id claim.");
    }
}

