using BCBGames.Application.DTOs;
using BCBGames.Domain.Entities;
using BCBGames.Domain.Events;
using BCBGames.Domain.Exceptions;
using BCBGames.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BCBGames.Application.Commands.Deposit;

public class DepositHandler : IRequestHandler<DepositCommand, TransactionResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxService _outboxService;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<DepositHandler> _logger;

    public DepositHandler(
        IUnitOfWork unitOfWork,
        IOutboxService outboxService,
        IIdempotencyService idempotencyService,
        ILogger<DepositHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _outboxService = outboxService;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<TransactionResponse> Handle(DepositCommand request, CancellationToken cancellationToken)
    {
        var idempotencyKey = request.IdempotencyKey ?? _idempotencyService.GenerateIdempotencyKey(
            request.AccountId,
            nameof(DepositedEvent),
            request.Amount);

        var response = await _unitOfWork.OrchestrateAsync<TransactionResponse>(
            async ct =>
            {
                var account = await _unitOfWork.Accounts.GetByIdForUpdateAsync(request.AccountId, ct)
                    ?? throw new AccountNotFoundException(request.AccountId);

                var (transaction, @event) = account.Deposit(
                    request.Amount,
                    request.Description,
                    idempotencyKey);

                await _unitOfWork.Transactions.AddAsync(transaction, ct);
                await _unitOfWork.Accounts.UpdateAsync(account, ct);
                await _outboxService.AddAsync(@event, ct);

                _logger.LogInformation(
                    "Deposit processed: AccountId={AccountId}, TransactionId={TransactionId}, Amount={Amount}, IdempotencyKey={IdempotencyKey}",
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
