using BCBGames.Application.DTOs;
using MediatR;

namespace BCBGames.Application.Queries.GetAccount;

public record GetAccountQuery(Guid UserId, Guid AccountId) : IRequest<AccountResponse?>;
