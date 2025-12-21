namespace BCBGames.API.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "BCBGames";
    public string Audience { get; init; } = "BCBGames";

    /// <summary>
    /// Symmetric key used to sign JWTs. Must be at least 32 chars for HS256 safety.
    /// Prefer setting via environment variable: Jwt__SigningKey
    /// </summary>
    public string SigningKey { get; init; } = "CHANGE_ME__PLEASE_SET_Jwt__SigningKey__32CHARS_MIN";

    public int AccessTokenMinutes { get; init; } = 60;
}

