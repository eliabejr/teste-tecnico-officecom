using System.Diagnostics;
using BCBGames.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BCBGames.Infrastructure.Cache;

public class RedisDistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDistributedLockService> _logger;
    private const string LockPrefix = "lock:";
    private const int DefaultExpirySeconds = 30;
    private const int DefaultWaitTimeSeconds = 10;

    public RedisDistributedLockService(
        IConnectionMultiplexer redis,
        ILogger<RedisDistributedLockService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<IDistributedLockHandle?> AcquireLockAsync(
        string key,
        TimeSpan? expiry = null,
        TimeSpan? waitTime = null,
        CancellationToken cancellationToken = default)
    {
        var lockKey = $"{LockPrefix}{key}";
        var lockValue = Guid.NewGuid().ToString();
        var expiryTime = expiry ?? TimeSpan.FromSeconds(DefaultExpirySeconds);
        var waitTimeSpan = waitTime ?? TimeSpan.FromSeconds(DefaultWaitTimeSeconds);
        var stopwatch = Stopwatch.StartNew();

        var database = _redis.GetDatabase();

        while (stopwatch.Elapsed < waitTimeSpan && !cancellationToken.IsCancellationRequested)
        {
            var acquired = await database.StringSetAsync(
                lockKey,
                lockValue,
                expiryTime,
                When.NotExists,
                CommandFlags.None);

            if (acquired)
            {
                _logger.LogDebug("Lock acquired: {LockKey}", lockKey);
                return new RedisDistributedLockHandle(database, lockKey, lockValue, expiryTime, _logger);
            }

            // Wait a bit before retrying
            await Task.Delay(Random.Shared.Next(50, 200), cancellationToken);
        }

        _logger.LogWarning("Failed to acquire lock: {LockKey} within {WaitTime}", lockKey, waitTimeSpan);
        return null;
    }

    public void Dispose()
    {
        // ConnectionMultiplexer is typically shared and disposed elsewhere
    }
}

internal class RedisDistributedLockHandle : IDistributedLockHandle
{
    private readonly IDatabase _database;
    private readonly string _lockKey;
    private readonly string _lockValue;
    private readonly TimeSpan _expiry;
    private readonly ILogger _logger;
    private bool _disposed;
    private bool _isAcquired = true;

    public RedisDistributedLockHandle(
        IDatabase database,
        string lockKey,
        string lockValue,
        TimeSpan expiry,
        ILogger logger)
    {
        _database = database;
        _lockKey = lockKey;
        _lockValue = lockValue;
        _expiry = expiry;
        _logger = logger;
    }

    public string LockKey => _lockKey;
    public bool IsAcquired => _isAcquired && !_disposed;

    public async Task<bool> ExtendAsync(TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        if (_disposed || !_isAcquired)
            return false;

        try
        {
            // Use Lua script to ensure we only extend if we still own the lock
            const string script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('pexpire', KEYS[1], ARGV[2])
                else
                    return 0
                end";

            var result = await _database.ScriptEvaluateAsync(
                script,
                new RedisKey[] { _lockKey },
                new RedisValue[] { _lockValue, (int)expiry.TotalMilliseconds });

            var extended = result.Resp2Type == ResultType.Integer && (int)result == 1;
            if (extended)
            {
                _logger.LogDebug("Lock extended: {LockKey}", _lockKey);
            }

            return extended;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extending lock: {LockKey}", _lockKey);
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            // Use Lua script to ensure we only release if we own the lock
            const string script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";

            _database.ScriptEvaluate(
                script,
                new RedisKey[] { _lockKey },
                new RedisValue[] { _lockValue });

            _isAcquired = false;
            _logger.LogDebug("Lock released: {LockKey}", _lockKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error releasing lock: {LockKey}", _lockKey);
        }
    }
}
