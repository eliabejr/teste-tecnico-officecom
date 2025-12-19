using BCBGames.Application.DTOs;
using MediatR;

namespace BCBGames.Application.Queries.RebuildBalance;

public record RebuildBalanceQuery(Guid AccountId) : IRequest<BalanceResponse?>;
