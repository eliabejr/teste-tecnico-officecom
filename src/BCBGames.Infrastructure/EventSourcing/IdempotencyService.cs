using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BCBGames.Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace BCBGames.Infrastructure.EventSourcing;

public class IdempotencyService : IIdempotencyService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<IdempotencyService> _logger;
    private const string IdempotencyKeyPrefix = "idempotency:";

    public IdempotencyService(
        IDistributedCache distributedCache,
        ILogger<IdempotencyService> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public string GenerateIdempotencyKey(
        Guid accountId,
        string transactionType,
        decimal amount,
        string? providedKey = null)
    {
        var normalizedAmount = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        string keyString;
        if (!string.IsNullOrWhiteSpace(providedKey))
        {
            keyString = $"{accountId}:{transactionType}:{normalizedAmount}:{providedKey.Trim()}";
        }
        else
        {
            var nonce = Guid.NewGuid();
            var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            keyString = $"{accountId}:{transactionType}:{normalizedAmount}:{timestampMs}:{nonce}";
        }

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

        _logger.LogDebug(
            "Generated idempotency key: {Key} for AccountId={AccountId}, Type={Type}, Amount={Amount}",
            hashString, accountId, transactionType, amount);

        return hashString;
    }

    public async Task<bool> IsDuplicateAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(idempotencyKey);
            var cachedValue = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);

            var isDuplicate = !string.IsNullOrEmpty(cachedValue);

            if (isDuplicate)
            {
                _logger.LogWarning(
                    "Duplicate idempotency key detected: {Key}",
                    idempotencyKey);
            }

            return isDuplicate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking idempotency key: {Key}", idempotencyKey);
            return false;
        }
    }

    public async Task<bool> StoreIdempotencyKeyAsync(
        string idempotencyKey,
        int ttlHours = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(idempotencyKey);

            var existing = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(existing))
            {
                return false;
            }

            var metadata = new
            {
                IdempotencyKey = idempotencyKey,
                StoredAt = DateTime.UtcNow,
                TtlHours = ttlHours
            };

            var serialized = JsonSerializer.Serialize(metadata);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(ttlHours)
            };

            await _distributedCache.SetStringAsync(cacheKey, serialized, options, cancellationToken);

            _logger.LogDebug(
                "Stored idempotency key: {Key} with TTL: {TtlHours}h",
                idempotencyKey, ttlHours);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing idempotency key: {Key}", idempotencyKey);
            return true;
        }
    }

    private static string GetCacheKey(string idempotencyKey) => $"{IdempotencyKeyPrefix}{idempotencyKey}";
}
