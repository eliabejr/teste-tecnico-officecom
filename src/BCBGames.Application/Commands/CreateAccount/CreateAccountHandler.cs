using BCBGames.Application.DTOs;
using BCBGames.Domain.Entities;
using BCBGames.Domain.Events;
using BCBGames.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BCBGames.Application.Commands.CreateAccount;

public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, AccountResponse>
{
    private readonly IEventStore _eventStore;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAccountHandler> _logger;

    public CreateAccountHandler(
        IEventStore eventStore,
        IIdempotencyService idempotencyService,
        IUnitOfWork unitOfWork,
        ILogger<CreateAccountHandler> logger)
    {
        _eventStore = eventStore;
        _idempotencyService = idempotencyService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AccountResponse> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = Account.Create(request.OwnerName, request.InitialBalance);

        var idempotencyKey = _idempotencyService.GenerateIdempotencyKey(
            account.Id,
            nameof(AccountCreatedEvent),
            request.InitialBalance);

        var @event = new AccountCreatedEvent(
            account.Id,
            account.AccountNumber,
            account.OwnerName,
            account.Balance,
            idempotencyKey);

        await _eventStore.PublishAsync(@event, cancellationToken);

        _logger.LogInformation(
            "AccountCreatedEvent published: AccountId={AccountId}, AccountNumber={AccountNumber}, IdempotencyKey={IdempotencyKey}",
            account.Id, account.AccountNumber, idempotencyKey);

        return MapToResponse(account);
    }

    private static AccountResponse MapToResponse(Account account) =>
        new(account.Id, account.AccountNumber, account.OwnerName,
            account.Balance, account.CreatedAt, account.UpdatedAt);
}
