namespace BCBGames.API.RateLimiting;

public sealed class RateLimitingOptions
{
    public bool Enabled { get; set; } = true;

    public bool FailOpen { get; set; } = true;

    public string KeyPrefix { get; set; } = "ratelimit";

    public int DefaultWindowSeconds { get; set; } = 60;

    public int DefaultPermitLimit { get; set; } = 300;

    public List<string> ExcludedPathPrefixes { get; set; } = new()
    {
        "/health",
        "/swagger"
    };

    public List<RateLimitRule> Rules { get; set; } = new();
}

public sealed class RateLimitRule
{
    public string PathPrefix { get; set; } = "/";

    public string[] Methods { get; set; } = Array.Empty<string>();

    public int WindowSeconds { get; set; } = 60;

    public int PermitLimit { get; set; } = 120;
}

