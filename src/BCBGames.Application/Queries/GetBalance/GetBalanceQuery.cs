using BCBGames.Application.DTOs;
using MediatR;

namespace BCBGames.Application.Queries.GetBalance;

public record GetBalanceQuery(Guid UserId, Guid AccountId) : IRequest<BalanceResponse?>;
