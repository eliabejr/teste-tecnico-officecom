using System.Net;
using System.Text.Json;
using BCBGames.API.RateLimiting;
using BCBGames.Application.DTOs;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BCBGames.API.Middleware;

public sealed class RateLimitingMiddleware
{
    private const string IncrementWithTtlScript = @"
local current = redis.call('INCR', KEYS[1])
if current == 1 then
  redis.call('PEXPIRE', KEYS[1], ARGV[1])
end
return current
";

    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<RateLimitingOptions> _options;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IOptionsMonitor<RateLimitingOptions> options,
        ILogger<RateLimitingMiddleware> logger,
        IConnectionMultiplexer? redis = null)
    {
        _next = next;
        _options = options;
        _logger = logger;
        _redis = redis;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var opt = _options.CurrentValue;
        if (!opt.Enabled)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (IsExcludedPath(opt, path))
        {
            await _next(context);
            return;
        }

        var rule = SelectRule(opt, path, context.Request.Method);
        var windowSeconds = Math.Max(1, rule?.WindowSeconds ?? opt.DefaultWindowSeconds);
        var permitLimit = Math.Max(1, rule?.PermitLimit ?? opt.DefaultPermitLimit);

        if (_redis is null)
        {
            if (!opt.FailOpen)
            {
                var nowEpochSecondsNoRedis = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var bucketNoRedis = nowEpochSecondsNoRedis / windowSeconds;
                var resetEpochSecondsNoRedis = (bucketNoRedis + 1) * windowSeconds;

                await WriteTooManyRequestsAsync(
                    context,
                    permitLimit: permitLimit,
                    remaining: 0,
                    resetEpochSeconds: resetEpochSecondsNoRedis,
                    retryAfterSeconds: Math.Max(1, (int)(resetEpochSecondsNoRedis - nowEpochSecondsNoRedis)));
                return;
            }

            await _next(context);
            return;
        }

        var nowEpochSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var bucket = nowEpochSeconds / windowSeconds;
        var resetEpochSeconds = (bucket + 1) * windowSeconds;

        var clientId = GetClientIdentifier(context);
        var ruleKey = rule is null ? "default" : NormalizeKey(rule.PathPrefix);

        var redisKey = $"{opt.KeyPrefix}:{ruleKey}:{clientId}:{bucket}";
        var ttlMs = windowSeconds * 2_000;

        long currentCount;
        try
        {
            var db = _redis.GetDatabase();
            var result = await db.ScriptEvaluateAsync(
                IncrementWithTtlScript,
                keys: new RedisKey[] { redisKey },
                values: new RedisValue[] { ttlMs });
            currentCount = (long)result;
        }
        catch (Exception ex)
        {
            if (opt.FailOpen)
            {
                _logger.LogWarning(ex, "Redis unavailable");
                await _next(context);
                return;
            }

            await WriteTooManyRequestsAsync(
                context,
                permitLimit: permitLimit,
                remaining: 0,
                resetEpochSeconds: resetEpochSeconds,
                retryAfterSeconds: Math.Max(1, (int)(resetEpochSeconds - nowEpochSeconds)));
            return;
        }

        var remaining = Math.Max(0, permitLimit - (int)currentCount);

        SetRateLimitHeaders(context.Response, permitLimit, remaining, resetEpochSeconds);

        if (currentCount > permitLimit)
        {
            var retryAfterSeconds = Math.Max(1, (int)(resetEpochSeconds - nowEpochSeconds));
            context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse(
                "Too many requests",
                $"Rate limit exceeded. Try again in {retryAfterSeconds}s.",
                429);

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
            return;
        }

        await _next(context);
    }

    private static bool IsExcludedPath(RateLimitingOptions opt, string path)
    {
        if (opt.ExcludedPathPrefixes.Count == 0) return false;
        for (var i = 0; i < opt.ExcludedPathPrefixes.Count; i++)
        {
            var prefix = opt.ExcludedPathPrefixes[i];
            if (string.IsNullOrWhiteSpace(prefix)) continue;
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static RateLimitRule? SelectRule(RateLimitingOptions opt, string path, string method)
    {
        if (opt.Rules.Count == 0) return null;

        RateLimitRule? best = null;
        var bestLen = -1;

        for (var i = 0; i < opt.Rules.Count; i++)
        {
            var rule = opt.Rules[i];
            if (string.IsNullOrWhiteSpace(rule.PathPrefix)) continue;
            if (!path.StartsWith(rule.PathPrefix, StringComparison.OrdinalIgnoreCase)) continue;

            if (rule.Methods.Length > 0 &&
                !rule.Methods.Any(m => string.Equals(m, method, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var len = rule.PathPrefix.Length;
            if (len > bestLen)
            {
                best = rule;
                bestLen = len;
            }
        }

        return best;
    }

    private static string GetClientIdentifier(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var xff))
        {
            var raw = xff.ToString();
            var first = raw.Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(first))
                return NormalizeKey(first);
        }

        var ip = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrWhiteSpace(ip))
            return NormalizeKey(ip);

        return NormalizeKey(context.TraceIdentifier);
    }

    private static void SetRateLimitHeaders(HttpResponse response, int permitLimit, int remaining, long resetEpochSeconds)
    {
        response.Headers["X-RateLimit-Limit"] = permitLimit.ToString();
        response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        response.Headers["X-RateLimit-Reset"] = resetEpochSeconds.ToString();
    }

    private static async Task WriteTooManyRequestsAsync(
        HttpContext context,
        int permitLimit,
        int remaining,
        long resetEpochSeconds,
        int retryAfterSeconds)
    {
        SetRateLimitHeaders(context.Response, permitLimit, remaining, resetEpochSeconds);
        context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();

        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse(
            "Too many requests",
            $"Rate limit exceeded. Try again in {retryAfterSeconds}s.",
            429);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }

    private static string NormalizeKey(string value)
    {
        return value
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace(":", "_")
            .Replace("/", "_");
    }
}

