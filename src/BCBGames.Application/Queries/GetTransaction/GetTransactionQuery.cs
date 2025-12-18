using BCBGames.Application.DTOs;
using MediatR;

namespace BCBGames.Application.Queries.GetTransaction;

public record GetTransactionQuery(Guid TransactionId) : IRequest<TransactionResponse?>;
