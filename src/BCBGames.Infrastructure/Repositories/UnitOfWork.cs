using BCBGames.Domain.Exceptions;
using BCBGames.Domain.Interfaces;
using BCBGames.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BCBGames.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    private IAccountRepository? _accounts;
    private ITransactionRepository? _transactions;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IAccountRepository Accounts => _accounts ??= new AccountRepository(_context);
    public ITransactionRepository Transactions => _transactions ??= new TransactionRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
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
                    await SaveChangesAsync(ct);
                    await CommitAsync(ct);
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

}
