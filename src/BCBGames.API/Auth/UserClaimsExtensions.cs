using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BCBGames.API.Auth;

public static class UserClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(sub, out var id)
            ? id
            : throw new InvalidOperationException("Authenticated user does not contain a valid 'sub' claim.");
    }
}

