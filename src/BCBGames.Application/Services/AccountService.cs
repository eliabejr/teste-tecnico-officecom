using BCBGames.Application.DTOs;
using BCBGames.Application.Interfaces;
using BCBGames.Domain.Entities;
using BCBGames.Domain.Exceptions;
using BCBGames.Domain.Interfaces;

namespace BCBGames.Application.Services;

public class AccountService : IAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public AccountService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<AccountResponse> CreateAccountAsync(CreateAccountRequest request, CancellationToken ct = default)
    {
        var account = Account.Create(request.OwnerName, request.InitialBalance);
        
        await _unitOfWork.Accounts.AddAsync(account, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        
        return MapToResponse(account);
    }
    
    public async Task<AccountResponse?> GetAccountAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(id, ct);
        return account is null ? null : MapToResponse(account);
    }
    
    public async Task<BalanceResponse?> GetBalanceAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(id, ct);
        
        if (account is null) return null;
        
        return new BalanceResponse(
            account.Id,
            account.AccountNumber,
            account.Balance,
            DateTime.UtcNow);
    }
    
    public async Task<StatementResponse?> GetStatementAsync(Guid id, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(id, ct);
        
        if (account is null) return null;
        
        var transactions = await _unitOfWork.Transactions.GetByAccountIdAsync(id, page, pageSize, ct);
        var totalCount = await _unitOfWork.Transactions.CountByAccountIdAsync(id, ct);
        
        return new StatementResponse(
            account.Id,
            account.AccountNumber,
            account.Balance,
            totalCount,
            page,
            pageSize,
            transactions.Select(MapToResponse));
    }
    
    private static AccountResponse MapToResponse(Account account) =>
        new(account.Id, account.AccountNumber, account.OwnerName, 
            account.Balance, account.CreatedAt, account.UpdatedAt);
    
    private static TransactionResponse MapToResponse(Domain.Entities.Transaction t) =>
        new(t.Id, t.AccountId, t.Type, t.Amount, t.Description, 
            t.BalanceBefore, t.BalanceAfter, t.Status, t.CreatedAt, t.ProcessedAt);
}
