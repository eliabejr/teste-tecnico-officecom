using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCBGames.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BCBGames.API.Auth;

public interface IJwtTokenService
{
    (string AccessToken, DateTime ExpiresAtUtc) CreateAccessToken(User user);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string AccessToken, DateTime ExpiresAtUtc) CreateAccessToken(User user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        };

        var keyBytes = Encoding.UTF8.GetBytes(_options.SigningKey);
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

