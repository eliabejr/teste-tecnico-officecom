using BCBGames.Application.DTOs;
using BCBGames.Domain.Interfaces;
using MediatR;

namespace BCBGames.Application.Queries.GetAccount;

public class GetAccountHandler : IRequestHandler<GetAccountQuery, AccountResponse?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAccountHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AccountResponse?> Handle(GetAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null) return null;
        if (account.UserId != request.UserId) return null;
        return MapToResponse(account);
    }

    private static AccountResponse MapToResponse(Domain.Entities.Account account) =>
        new(account.Id, account.AccountNumber, account.OwnerName,
            account.Balance, account.CreatedAt, account.UpdatedAt);
}
