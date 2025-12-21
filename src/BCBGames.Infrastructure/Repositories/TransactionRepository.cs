using BCBGames.Domain.Entities;
using BCBGames.Domain.Interfaces;
using BCBGames.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BCBGames.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _context;

    public TransactionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Transaction?> GetByIdForUserAsync(
        Guid transactionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.Id == transactionId && t.Account != null && t.Account.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
        return transaction;
    }

    public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(
        Guid accountId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .CountAsync(t => t.AccountId == accountId, cancellationToken);
    }
}
