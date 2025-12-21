using BCBGames.Domain.Interfaces;

namespace BCBGames.IntegrationTests.Infrastructure;

/// <summary>
/// No-op cache for integration tests (avoids Redis dependency and timeouts).
/// </summary>
public sealed class NoOpCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        => Task.FromResult<T?>(null);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

