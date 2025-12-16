using BCBGames.Domain.Entities;

namespace BCBGames.Domain.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Account?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default);
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByAccountIdAsync(
        Guid accountId, 
        int page = 1, 
        int pageSize = 50, 
        CancellationToken cancellationToken = default);
    Task<int> CountByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork : IDisposable
{
    IAccountRepository Accounts { get; }
    ITransactionRepository Transactions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
