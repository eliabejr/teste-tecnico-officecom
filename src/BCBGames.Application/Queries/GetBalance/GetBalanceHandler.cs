using BCBGames.Application.DTOs;
using BCBGames.Domain.Interfaces;
using MediatR;

namespace BCBGames.Application.Queries.GetBalance;

public class GetBalanceHandler : IRequestHandler<GetBalanceQuery, BalanceResponse?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBalanceHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BalanceResponse?> Handle(GetBalanceQuery request, CancellationToken cancellationToken)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null) return null;

        return new BalanceResponse(
            account.Id,
            account.AccountNumber,
            account.Balance,
            DateTime.UtcNow);
    }
}
