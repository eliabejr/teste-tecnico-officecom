using BCBGames.Application.DTOs;
using BCBGames.Domain.Interfaces;
using MediatR;

namespace BCBGames.Application.Queries.GetTransaction;

public class GetTransactionHandler : IRequestHandler<GetTransactionQuery, TransactionResponse?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTransactionHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TransactionResponse?> Handle(GetTransactionQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(request.TransactionId, cancellationToken);
        return transaction is null ? null : MapToResponse(transaction);
    }

    private static TransactionResponse MapToResponse(Domain.Entities.Transaction t) =>
        new(t.Id, t.AccountId, t.Type, t.Amount, t.Description,
            t.BalanceBefore, t.BalanceAfter, t.Status, t.CreatedAt, t.ProcessedAt);
}
