using BCBGames.Application.DTOs;
using BCBGames.Domain.Interfaces;
using MediatR;

namespace BCBGames.Application.Queries.GetStatement;

public class GetStatementHandler : IRequestHandler<GetStatementQuery, StatementResponse?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetStatementHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<StatementResponse?> Handle(GetStatementQuery request, CancellationToken cancellationToken)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null) return null;

        var transactions = await _unitOfWork.Transactions.GetByAccountIdAsync(
            request.AccountId, request.Page, request.PageSize, cancellationToken);
        var totalCount = await _unitOfWork.Transactions.CountByAccountIdAsync(request.AccountId, cancellationToken);

        return new StatementResponse(
            account.Id,
            account.AccountNumber,
            account.Balance,
            totalCount,
            request.Page,
            request.PageSize,
            transactions.Select(MapToResponse));
    }

    private static TransactionResponse MapToResponse(Domain.Entities.Transaction t) =>
        new(t.Id, t.AccountId, t.Type, t.Amount, t.Description,
            t.BalanceBefore, t.BalanceAfter, t.Status, t.CreatedAt, t.ProcessedAt);
}
