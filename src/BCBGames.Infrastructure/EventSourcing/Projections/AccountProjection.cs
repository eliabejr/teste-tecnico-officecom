using System.Reflection;
using BCBGames.Domain.Entities;
using BCBGames.Domain.Enums;
using BCBGames.Domain.Events;
using BCBGames.Domain.Interfaces;
using BCBGames.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BCBGames.Infrastructure.EventSourcing.Projections;

public class AccountProjection
{
    private readonly AppDbContext _context;
    private readonly ILogger<AccountProjection> _logger;

    public AccountProjection(
        AppDbContext context,
        ILogger<AccountProjection> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task HandleAsync(AccountCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingAccount = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == @event.AggregateId, cancellationToken);

            if (existingAccount != null)
            {
                _logger.LogWarning(
                    "Account {AccountId} already exists. Skipping projection for AccountCreatedEvent.",
                    @event.AggregateId);
                return;
            }

            var account = Account.Create(@event.OwnerName, @event.InitialBalance);

            var accountType = typeof(Account);
            accountType.GetProperty(nameof(Account.Id))!
                .SetValue(account, @event.AggregateId);
            accountType.GetProperty(nameof(Account.AccountNumber))!
                .SetValue(account, @event.AccountNumber);
            accountType.GetProperty(nameof(Account.CreatedAt))!
                .SetValue(account, @event.Timestamp);
            accountType.GetProperty(nameof(Account.UpdatedAt))!
                .SetValue(account, @event.Timestamp);

            await _context.Accounts.AddAsync(account, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Account projection created: AccountId={AccountId}, AccountNumber={AccountNumber}, Balance={Balance}",
                account.Id, account.AccountNumber, account.Balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling AccountCreatedEvent for AccountId={AccountId}",
                @event.AggregateId);
            throw;
        }
    }

    public async Task HandleAsync(DepositedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingTransaction = await _context.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == @event.TransactionId, cancellationToken);

            if (existingTransaction != null)
            {
                _logger.LogWarning(
                    "Transaction {TransactionId} already exists. Skipping projection for DepositedEvent.",
                    @event.TransactionId);
                return;
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == @event.AggregateId, cancellationToken);

            if (account == null)
            {
                _logger.LogError(
                    "Account {AccountId} not found for DepositedEvent",
                    @event.AggregateId);
                throw new InvalidOperationException($"Account {@event.AggregateId} not found");
            }

            account.Credit(@event.Amount);
            account.GetType().GetProperty(nameof(Account.UpdatedAt))!
                .SetValue(account, @event.Timestamp);

            var transaction = Transaction.Create(
                @event.AggregateId,
                TransactionType.Deposit,
                @event.Amount,
                @event.BalanceBefore,
                @event.Description ?? "Depósito");

            transaction.GetType().GetProperty(nameof(Transaction.Id))!
                .SetValue(transaction, @event.TransactionId);
            transaction.GetType().GetProperty(nameof(Transaction.CreatedAt))!
                .SetValue(transaction, @event.Timestamp);
            transaction.Complete(@event.BalanceAfter);
            transaction.GetType().GetProperty(nameof(Transaction.ProcessedAt))!
                .SetValue(transaction, @event.Timestamp);

            await _context.Transactions.AddAsync(transaction, cancellationToken);
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Deposit projection applied: AccountId={AccountId}, TransactionId={TransactionId}, Amount={Amount}, NewBalance={Balance}",
                account.Id, @event.TransactionId, @event.Amount, account.Balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling DepositedEvent for AccountId={AccountId}, TransactionId={TransactionId}",
                @event.AggregateId, @event.TransactionId);
            throw;
        }
    }

    public async Task HandleAsync(WithdrawnEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingTransaction = await _context.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == @event.TransactionId, cancellationToken);

            if (existingTransaction != null)
            {
                _logger.LogWarning(
                    "Transaction {TransactionId} already exists. Skipping projection for WithdrawnEvent.",
                    @event.TransactionId);
                return;
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == @event.AggregateId, cancellationToken);

            if (account == null)
            {
                _logger.LogError(
                    "Account {AccountId} not found for WithdrawnEvent",
                    @event.AggregateId);
                throw new InvalidOperationException($"Account {@event.AggregateId} not found");
            }

            account.Debit(@event.Amount);
            account.GetType().GetProperty(nameof(Account.UpdatedAt))!
                .SetValue(account, @event.Timestamp);

            var transaction = Transaction.Create(
                @event.AggregateId,
                TransactionType.Withdraw,
                @event.Amount,
                @event.BalanceBefore,
                @event.Description ?? "Saque");

            transaction.GetType().GetProperty(nameof(Transaction.Id))!
                .SetValue(transaction, @event.TransactionId);
            transaction.GetType().GetProperty(nameof(Transaction.CreatedAt))!
                .SetValue(transaction, @event.Timestamp);
            transaction.Complete(@event.BalanceAfter);
            transaction.GetType().GetProperty(nameof(Transaction.ProcessedAt))!
                .SetValue(transaction, @event.Timestamp);

            await _context.Transactions.AddAsync(transaction, cancellationToken);
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Withdrawal projection applied: AccountId={AccountId}, TransactionId={TransactionId}, Amount={Amount}, NewBalance={Balance}",
                account.Id, @event.TransactionId, @event.Amount, account.Balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling WithdrawnEvent for AccountId={AccountId}, TransactionId={TransactionId}",
                @event.AggregateId, @event.TransactionId);
            throw;
        }
    }

    public async Task HandleAsync(PurchasedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingTransaction = await _context.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == @event.TransactionId, cancellationToken);

            if (existingTransaction != null)
            {
                _logger.LogWarning(
                    "Transaction {TransactionId} already exists. Skipping projection for PurchasedEvent.",
                    @event.TransactionId);
                return;
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == @event.AggregateId, cancellationToken);

            if (account == null)
            {
                _logger.LogError(
                    "Account {AccountId} not found for PurchasedEvent",
                    @event.AggregateId);
                throw new InvalidOperationException($"Account {@event.AggregateId} not found");
            }

            account.Debit(@event.Amount);
            account.GetType().GetProperty(nameof(Account.UpdatedAt))!
                .SetValue(account, @event.Timestamp);

            var transaction = Transaction.Create(
                @event.AggregateId,
                TransactionType.Purchase,
                @event.Amount,
                @event.BalanceBefore,
                $"Compra: {@event.Merchant}");

            transaction.GetType().GetProperty(nameof(Transaction.Id))!
                .SetValue(transaction, @event.TransactionId);
            transaction.GetType().GetProperty(nameof(Transaction.CreatedAt))!
                .SetValue(transaction, @event.Timestamp);
            transaction.Complete(@event.BalanceAfter);
            transaction.GetType().GetProperty(nameof(Transaction.ProcessedAt))!
                .SetValue(transaction, @event.Timestamp);

            await _context.Transactions.AddAsync(transaction, cancellationToken);
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Purchase projection applied: AccountId={AccountId}, TransactionId={TransactionId}, Amount={Amount}, Merchant={Merchant}, NewBalance={Balance}",
                account.Id, @event.TransactionId, @event.Amount, @event.Merchant, account.Balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling PurchasedEvent for AccountId={AccountId}, TransactionId={TransactionId}",
                @event.AggregateId, @event.TransactionId);
            throw;
        }
    }
}
