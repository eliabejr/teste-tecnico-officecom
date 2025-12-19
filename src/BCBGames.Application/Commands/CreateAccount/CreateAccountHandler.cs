using BCBGames.Application.DTOs;
using BCBGames.Domain.Entities;
using BCBGames.Domain.Events;
using BCBGames.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BCBGames.Application.Commands.CreateAccount;

public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, AccountResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxService _outboxService;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<CreateAccountHandler> _logger;

    public CreateAccountHandler(
        IUnitOfWork unitOfWork,
        IOutboxService outboxService,
        IIdempotencyService idempotencyService,
        ILogger<CreateAccountHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _outboxService = outboxService;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<AccountResponse> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var response = await _unitOfWork.OrchestrateAsync(
            async ct =>
            {
                var account = Account.Create(request.OwnerName, request.InitialBalance);

                var idempotencyKey = request.IdempotencyKey ?? _idempotencyService.GenerateIdempotencyKey(
                    account.Id,
                    nameof(AccountCreatedEvent),
                    request.InitialBalance);

                var @event = new AccountCreatedEvent(
                    account.Id,
                    account.AccountNumber,
                    account.OwnerName,
                    account.Balance,
                    idempotencyKey);

                await _unitOfWork.Accounts.AddAsync(account, ct);
                await _outboxService.AddAsync(@event, ct);

                _logger.LogInformation(
                    "Account created: AccountId={AccountId}, AccountNumber={AccountNumber}, IdempotencyKey={IdempotencyKey}",
                    account.Id, account.AccountNumber, idempotencyKey);

                return MapToResponse(account);
            },
            cancellationToken);

        return response;
    }

    private static AccountResponse MapToResponse(Account account) =>
        new(account.Id, account.AccountNumber, account.OwnerName,
            account.Balance, account.CreatedAt, account.UpdatedAt);
}
