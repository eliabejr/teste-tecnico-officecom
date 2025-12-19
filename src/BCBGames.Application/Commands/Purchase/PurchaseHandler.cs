using System.Reflection;
using BCBGames.Application.DTOs;
using BCBGames.Domain.Entities;
using BCBGames.Domain.Enums;
using BCBGames.Domain.Events;
using BCBGames.Domain.Exceptions;
using BCBGames.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BCBGames.Application.Commands.Purchase;

public class PurchaseHandler : IRequestHandler<PurchaseCommand, TransactionResponse>
{
    private readonly IEventStore _eventStore;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PurchaseHandler> _logger;

    public PurchaseHandler(
        IEventStore eventStore,
        IIdempotencyService idempotencyService,
        IUnitOfWork unitOfWork,
        ILogger<PurchaseHandler> logger)
    {
        _eventStore = eventStore;
        _idempotencyService = idempotencyService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TransactionResponse> Handle(PurchaseCommand request, CancellationToken cancellationToken)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new AccountNotFoundException(request.AccountId);

        if (!account.HasSufficientBalance(request.Amount))
            throw new InsufficientBalanceException(account.Balance, request.Amount);

        var idempotencyKey = _idempotencyService.GenerateIdempotencyKey(
            request.AccountId,
            nameof(PurchasedEvent),
            request.Amount);

        var transactionId = Guid.NewGuid();
        var transaction = Transaction.Create(
            account.Id,
            TransactionType.Purchase,
            request.Amount,
            account.Balance,
            $"Compra: {request.Merchant}");

        typeof(Transaction).GetProperty(nameof(Transaction.Id))!
            .SetValue(transaction, transactionId);

        var balanceAfter = account.Balance - request.Amount;

        var @event = new PurchasedEvent(
            account.Id,
            transactionId,
            request.Amount,
            request.Merchant,
            account.Balance,
            balanceAfter,
            idempotencyKey);

        await _eventStore.PublishAsync(@event, cancellationToken);

        _logger.LogInformation(
            "PurchasedEvent published: AccountId={AccountId}, TransactionId={TransactionId}, Amount={Amount}, Merchant={Merchant}, IdempotencyKey={IdempotencyKey}",
            account.Id, transactionId, request.Amount, request.Merchant, idempotencyKey);

        transaction.Complete(balanceAfter);
        return MapToResponse(transaction);
    }

    private static TransactionResponse MapToResponse(Transaction t) =>
        new(t.Id, t.AccountId, t.Type, t.Amount, t.Description,
            t.BalanceBefore, t.BalanceAfter, t.Status, t.CreatedAt, t.ProcessedAt);
}
