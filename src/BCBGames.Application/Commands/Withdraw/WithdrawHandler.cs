using BCBGames.Application.DTOs;
using BCBGames.Domain.Entities;
using BCBGames.Domain.Events;
using BCBGames.Domain.Exceptions;
using BCBGames.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BCBGames.Application.Commands.Withdraw;

public class WithdrawHandler : IRequestHandler<WithdrawCommand, TransactionResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxService _outboxService;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<WithdrawHandler> _logger;
    private const string LockKeyPrefix = "account-lock:";

    public WithdrawHandler(
        IUnitOfWork unitOfWork,
        IOutboxService outboxService,
        IIdempotencyService idempotencyService,
        ILogger<WithdrawHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _outboxService = outboxService;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<TransactionResponse> Handle(WithdrawCommand request, CancellationToken cancellationToken)
    {
        var lockKey = $"{LockKeyPrefix}{request.AccountId}";
        var idempotencyKey = request.IdempotencyKey ?? _idempotencyService.GenerateIdempotencyKey(
            request.AccountId,
            nameof(WithdrawnEvent),
            request.Amount);

        var response = await _unitOfWork.OrchestrateAsync<TransactionResponse>(
            lockKey,
            async ct =>
            {
                var account = await _unitOfWork.Accounts.GetByIdForUpdateAsync(request.AccountId, ct)
                    ?? throw new AccountNotFoundException(request.AccountId);

                var (transaction, @event) = account.Withdraw(
                    request.Amount,
                    request.Description,
                    idempotencyKey);

                await _unitOfWork.Transactions.AddAsync(transaction, ct);
                await _unitOfWork.Accounts.UpdateAsync(account, ct);
                await _outboxService.AddAsync(@event, ct);

                _logger.LogInformation(
                    "Withdrawal processed: AccountId={AccountId}, TransactionId={TransactionId}, Amount={Amount}, IdempotencyKey={IdempotencyKey}",
                    account.Id, transaction.Id, request.Amount, idempotencyKey);

                return MapToResponse(transaction);
            },
            cancellationToken);

        return response;
    }

    private static TransactionResponse MapToResponse(Transaction t) =>
        new(t.Id, t.AccountId, t.Type, t.Amount, t.Description,
            t.BalanceBefore, t.BalanceAfter, t.Status, t.CreatedAt, t.ProcessedAt);
}
