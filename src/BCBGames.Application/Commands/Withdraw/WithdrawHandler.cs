using BCBGames.Application.DTOs;
using BCBGames.Domain.Entities;
using BCBGames.Domain.Enums;
using BCBGames.Domain.Exceptions;
using BCBGames.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BCBGames.Application.Commands.Withdraw;

public class WithdrawHandler : IRequestHandler<WithdrawCommand, TransactionResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WithdrawHandler> _logger;

    public WithdrawHandler(IUnitOfWork unitOfWork, ILogger<WithdrawHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<TransactionResponse> Handle(WithdrawCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.OrchestrateAsync(async ct =>
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

            _logger.LogInformation(
                "Withdraw completed: Account={AccountId}, Amount={Amount}, NewBalance={Balance}",
                account.Id, request.Amount, account.Balance);

            return MapToResponse(transaction);
        }, cancellationToken);
    }

    private static TransactionResponse MapToResponse(Transaction t) =>
        new(t.Id, t.AccountId, t.Type, t.Amount, t.Description,
            t.BalanceBefore, t.BalanceAfter, t.Status, t.CreatedAt, t.ProcessedAt);
}
