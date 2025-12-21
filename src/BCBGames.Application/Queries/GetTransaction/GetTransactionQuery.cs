using BCBGames.Application.DTOs;
using MediatR;

namespace BCBGames.Application.Queries.GetTransaction;

public record GetTransactionQuery(Guid UserId, Guid TransactionId) : IRequest<TransactionResponse?>;
