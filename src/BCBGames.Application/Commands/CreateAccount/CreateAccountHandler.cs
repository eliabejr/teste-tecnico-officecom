using BCBGames.Application.DTOs;
using BCBGames.Domain.Entities;
using BCBGames.Domain.Interfaces;
using MediatR;

namespace BCBGames.Application.Commands.CreateAccount;

public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, AccountResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateAccountHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AccountResponse> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = Account.Create(request.OwnerName, request.InitialBalance);

        await _unitOfWork.Accounts.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(account);
    }

    private static AccountResponse MapToResponse(Account account) =>
        new(account.Id, account.AccountNumber, account.OwnerName,
            account.Balance, account.CreatedAt, account.UpdatedAt);
}
