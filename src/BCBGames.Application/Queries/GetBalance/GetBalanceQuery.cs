using BCBGames.Application.DTOs;
using MediatR;

namespace BCBGames.Application.Queries.GetBalance;

public record GetBalanceQuery(Guid AccountId) : IRequest<BalanceResponse?>;
