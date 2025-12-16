using BCBGames.Application.DTOs;
using BCBGames.Application.Interfaces;
using BCBGames.Domain.Entities;
using BCBGames.Domain.Enums;
using BCBGames.Domain.Exceptions;
using BCBGames.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BCBGames.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionService> _logger;
    private const int MaxRetries = 3;
    
    public TransactionService(IUnitOfWork unitOfWork, ILogger<TransactionService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<TransactionResponse> DepositAsync(DepositRequest request, CancellationToken ct = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            
            try
            {
                var account = await _unitOfWork.Accounts.GetByIdForUpdateAsync(request.AccountId, ct)
                    ?? throw new AccountNotFoundException(request.AccountId);
                
                var transaction = Transaction.Create(
                    account.Id,
                    TransactionType.Deposit,
                    request.Amount,
                    account.Balance,
                    request.Description ?? "Depósito");
                
                account.Credit(request.Amount);
                transaction.Complete(account.Balance);
                
                await _unitOfWork.Transactions.AddAsync(transaction, ct);
                await _unitOfWork.Accounts.UpdateAsync(account, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitAsync(ct);
                
                _logger.LogInformation(
                    "Deposit completed: Account={AccountId}, Amount={Amount}, NewBalance={Balance}",
                    account.Id, request.Amount, account.Balance);
                
                return MapToResponse(transaction);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(ct);
                throw;
            }
        }, ct);
    }
    
    public async Task<TransactionResponse> WithdrawAsync(WithdrawRequest request, CancellationToken ct = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            
            try
            {
                var account = await _unitOfWork.Accounts.GetByIdForUpdateAsync(request.AccountId, ct)
                    ?? throw new AccountNotFoundException(request.AccountId);
                
                if (!account.HasSufficientBalance(request.Amount))
                    throw new InsufficientBalanceException(account.Balance, request.Amount);
                
                var transaction = Transaction.Create(
                    account.Id,
                    TransactionType.Withdraw,
                    request.Amount,
                    account.Balance,
                    request.Description ?? "Saque");
                
                account.Debit(request.Amount);
                transaction.Complete(account.Balance);
                
                await _unitOfWork.Transactions.AddAsync(transaction, ct);
                await _unitOfWork.Accounts.UpdateAsync(account, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitAsync(ct);
                
                _logger.LogInformation(
                    "Withdraw completed: Account={AccountId}, Amount={Amount}, NewBalance={Balance}",
                    account.Id, request.Amount, account.Balance);
                
                return MapToResponse(transaction);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(ct);
                throw;
            }
        }, ct);
    }
    
    public async Task<TransactionResponse> PurchaseAsync(PurchaseRequest request, CancellationToken ct = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            
            try
            {
                var account = await _unitOfWork.Accounts.GetByIdForUpdateAsync(request.AccountId, ct)
                    ?? throw new AccountNotFoundException(request.AccountId);
                
                if (!account.HasSufficientBalance(request.Amount))
                    throw new InsufficientBalanceException(account.Balance, request.Amount);
                
                var transaction = Transaction.Create(
                    account.Id,
                    TransactionType.Purchase,
                    request.Amount,
                    account.Balance,
                    $"Compra: {request.Merchant}");
                
                account.Debit(request.Amount);
                transaction.Complete(account.Balance);
                
                await _unitOfWork.Transactions.AddAsync(transaction, ct);
                await _unitOfWork.Accounts.UpdateAsync(account, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitAsync(ct);
                
                _logger.LogInformation(
                    "Purchase completed: Account={AccountId}, Merchant={Merchant}, Amount={Amount}, NewBalance={Balance}",
                    account.Id, request.Merchant, request.Amount, account.Balance);
                
                return MapToResponse(transaction);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(ct);
                throw;
            }
        }, ct);
    }
    
    public async Task<TransactionResponse?> GetTransactionAsync(Guid id, CancellationToken ct = default)
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(id, ct);
        return transaction is null ? null : MapToResponse(transaction);
    }
    
    private async Task<TransactionResponse> ExecuteWithRetryAsync(
        Func<Task<TransactionResponse>> operation,
        CancellationToken ct)
    {
        var retryCount = 0;
        
        while (true)
        {
            try
            {
                return await operation();
            }
            catch (ConcurrencyException) when (retryCount < MaxRetries)
            {
                retryCount++;
                _logger.LogWarning(
                    "Concurrency conflict detected, retrying... Attempt {Attempt} of {MaxRetries}",
                    retryCount, MaxRetries);
                
                await Task.Delay(Random.Shared.Next(10, 50), ct);
            }
        }
    }
    
    private static TransactionResponse MapToResponse(Transaction t) =>
        new(t.Id, t.AccountId, t.Type, t.Amount, t.Description, 
            t.BalanceBefore, t.BalanceAfter, t.Status, t.CreatedAt, t.ProcessedAt);
}
