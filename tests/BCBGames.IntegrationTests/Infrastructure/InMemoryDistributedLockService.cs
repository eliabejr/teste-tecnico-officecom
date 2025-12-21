using System.Collections.Concurrent;
using BCBGames.Domain.Interfaces;

namespace BCBGames.IntegrationTests.Infrastructure;

/// <summary>
/// In-memory lock service for integration tests (single-process).
/// Avoids requiring Redis while keeping the application code path the same.
/// </summary>
public sealed class InMemoryDistributedLockService : IDistributedLockService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    public async Task<IDistributedLockHandle?> AcquireLockAsync(
        string key,
        TimeSpan? expiry = null,
        TimeSpan? waitTime = null,
        CancellationToken cancellationToken = default)
    {
        var semaphore = Locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        var maxWait = waitTime ?? TimeSpan.FromSeconds(10);

        var acquired = await semaphore.WaitAsync(maxWait, cancellationToken);
        if (!acquired) return null;

        return new InMemoryDistributedLockHandle(key, semaphore);
    }

    public void Dispose()
    {
        // no-op for tests
    }

    private sealed class InMemoryDistributedLockHandle : IDistributedLockHandle
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public InMemoryDistributedLockHandle(string key, SemaphoreSlim semaphore)
        {
            LockKey = key;
            _semaphore = semaphore;
        }

        public string LockKey { get; }
        public bool IsAcquired => !_disposed;

        public Task<bool> ExtendAsync(TimeSpan expiry, CancellationToken cancellationToken = default)
        {
            // Not needed for in-memory test locks.
            return Task.FromResult(!_disposed);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _semaphore.Release();
        }
    }
}

