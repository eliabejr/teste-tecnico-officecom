using BCBGames.Application.DTOs;
using MediatR;

namespace BCBGames.Application.Queries.GetAccount;

public record GetAccountQuery(Guid AccountId) : IRequest<AccountResponse?>;
