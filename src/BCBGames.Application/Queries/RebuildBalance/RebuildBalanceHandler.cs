using BCBGames.Application.DTOs;
using BCBGames.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BCBGames.Application.Queries.RebuildBalance;

public class RebuildBalanceHandler : IRequestHandler<RebuildBalanceQuery, BalanceResponse?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RebuildBalanceHandler> _logger;

    public RebuildBalanceHandler(
        IUnitOfWork unitOfWork,
        ILogger<RebuildBalanceHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BalanceResponse?> Handle(RebuildBalanceQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "RebuildBalance called for AccountId={AccountId}. Reading from PostgreSQL (synchronous projection).",
            request.AccountId);

        try
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);

            if (account == null)
            {
                _logger.LogInformation("Account not found: AccountId={AccountId}", request.AccountId);
                return null;
            }

            _logger.LogInformation(
                "Balance retrieved for AccountId={AccountId}. Balance={Balance}",
                request.AccountId, account.Balance);

            return new BalanceResponse(
                account.Id,
                account.AccountNumber,
                account.Balance,
                account.UpdatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving balance for AccountId={AccountId}", request.AccountId);
            throw;
        }
    }
}
