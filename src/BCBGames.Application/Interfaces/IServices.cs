using BCBGames.Application.DTOs;

namespace BCBGames.Application.Interfaces;

public interface IAccountService
{
    Task<AccountResponse> CreateAccountAsync(CreateAccountRequest request, CancellationToken ct = default);
    Task<AccountResponse?> GetAccountAsync(Guid id, CancellationToken ct = default);
    Task<BalanceResponse?> GetBalanceAsync(Guid id, CancellationToken ct = default);
    Task<StatementResponse?> GetStatementAsync(Guid id, int page = 1, int pageSize = 50, CancellationToken ct = default);
}

public interface ITransactionService
{
    Task<TransactionResponse> DepositAsync(DepositRequest request, CancellationToken ct = default);
    Task<TransactionResponse> WithdrawAsync(WithdrawRequest request, CancellationToken ct = default);
    Task<TransactionResponse> PurchaseAsync(PurchaseRequest request, CancellationToken ct = default);
    Task<TransactionResponse?> GetTransactionAsync(Guid id, CancellationToken ct = default);
}
