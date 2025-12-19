using BCBGames.Domain.Exceptions;
using BCBGames.Domain.Entities;
using BCBGames.Domain.Interfaces;
using BCBGames.Infrastructure.Cache;
using BCBGames.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace BCBGames.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly IDistributedLockService _lockService;
    private readonly ILogger<AccountRepository> _accountLogger;
    private IDbContextTransaction? _transaction;

    private IAccountRepository? _accounts;
    private ITransactionRepository? _transactions;

    public UnitOfWork(
        AppDbContext context,
        ICacheService cacheService,
        IDistributedLockService lockService,
        ILogger<AccountRepository> accountLogger)
    {
        _context = context;
        _cacheService = cacheService;
        _lockService = lockService;
        _accountLogger = accountLogger;
    }

    public IAccountRepository Accounts => _accounts ??= new AccountRepository(_context, _cacheService, _accountLogger);
    public ITransactionRepository Transactions => _transactions ??= new TransactionRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var changedAccounts = _context.ChangeTracker
                .Entries<Account>()
                .Where(e => e.State is EntityState.Added or EntityState.Modified)
                .Select(e => e.Entity)
                .ToList();

            var rows = await _context.SaveChangesAsync(cancellationToken);

            if (_transaction is null && changedAccounts.Count != 0)
            {
                await UpdateAccountsCacheAsync(changedAccounts, cancellationToken);
            }

            return rows;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException();
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }

    public async Task<TResult> OrchestrateAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default)
    {
        var retryCount = 0;
        const int baseDelayMs = 5;
        const int maxDelayMs = 100;

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            while (true)
            {
                _context.ChangeTracker.Clear();

                if (_transaction is not null)
                {
                    await RollbackAsync(ct);
                }

                await BeginTransactionAsync(ct);

                try
                {
                    var result = await operation(ct);

                    var changedAccounts = _context.ChangeTracker
                        .Entries<Account>()
                        .Where(e => e.State is EntityState.Added or EntityState.Modified)
                        .Select(e => e.Entity)
                        .ToList();

                    await SaveChangesAsync(ct);
                    await CommitAsync(ct);

                    if (changedAccounts.Count != 0)
                    {
                        await UpdateAccountsCacheAsync(changedAccounts, ct);
                    }

                    return result;
                }
                catch (ConcurrencyException)
                {
                    retryCount++;
                    var delay = Math.Min(baseDelayMs * (1 << Math.Min(retryCount, 6)), maxDelayMs);
                    await Task.Delay(Random.Shared.Next(delay / 2, delay), ct);
                }
                catch
                {
                    await RollbackAsync(ct);
                    throw;
                }
            }
        });
    }

    public async Task<TResult> OrchestrateAsync<TResult>(
        string lockKey,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default)
    {
        var retryCount = 0;
        const int baseDelayMs = 5;
        const int maxDelayMs = 100;
        var lockExpiry = TimeSpan.FromSeconds(30);
        var lockWaitTime = TimeSpan.FromSeconds(10);

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            while (true)
            {
                IDistributedLockHandle? lockHandle = null;

                try
                {
                    lockHandle = await _lockService.AcquireLockAsync(
                        lockKey,
                        lockExpiry,
                        lockWaitTime,
                        ct);

                    if (lockHandle == null || !lockHandle.IsAcquired)
                    {
                        throw new InvalidOperationException($"Could not acquire lock for key: {lockKey}");
                    }

                    _context.ChangeTracker.Clear();

                    if (_transaction is not null)
                    {
                        await RollbackAsync(ct);
                    }

                    await BeginTransactionAsync(ct);

                    try
                    {
                        var result = await operation(ct);

                        var changedAccounts = _context.ChangeTracker
                            .Entries<Account>()
                            .Where(e => e.State is EntityState.Added or EntityState.Modified)
                            .Select(e => e.Entity)
                            .ToList();

                        await SaveChangesAsync(ct);
                        await CommitAsync(ct);

                        lockHandle?.Dispose();
                        lockHandle = null;

                        if (changedAccounts.Count != 0)
                        {
                            await UpdateAccountsCacheAsync(changedAccounts, ct);
                        }

                        return result;
                    }
                    catch (ConcurrencyException)
                    {
                        await RollbackAsync(ct);
                        lockHandle?.Dispose();
                        lockHandle = null;

                        retryCount++;
                        var delay = Math.Min(baseDelayMs * (1 << Math.Min(retryCount, 6)), maxDelayMs);
                        await Task.Delay(Random.Shared.Next(delay / 2, delay), ct);
                    }
                    catch
                    {
                        await RollbackAsync(ct);
                        lockHandle?.Dispose();
                        throw;
                    }
                }
                catch (Exception) when (lockHandle != null)
                {
                    lockHandle.Dispose();
                    throw;
                }
            }
        });
    }

    private async Task UpdateAccountsCacheAsync(IEnumerable<Account> accounts, CancellationToken cancellationToken)
    {
        const string cacheKeyPrefix = "account:";
        var expiration = TimeSpan.FromMinutes(5);

        foreach (var account in accounts)
        {
            var cacheKey = $"{cacheKeyPrefix}{account.Id}";
            var dto = new AccountCacheDto
            {
                Id = account.Id,
                AccountNumber = account.AccountNumber,
                OwnerName = account.OwnerName,
                Balance = account.Balance,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt,
                Version = account.Version
            };

            await _cacheService.SetAsync(cacheKey, dto, expiration, cancellationToken);
        }
    }

}
