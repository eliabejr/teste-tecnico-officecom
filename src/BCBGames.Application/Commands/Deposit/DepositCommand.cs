using BCBGames.Application.DTOs;
using MediatR;

namespace BCBGames.Application.Commands.Deposit;

public record DepositCommand(
    Guid AccountId,
    decimal Amount,
    string? Description = null,
    string? IdempotencyKey = null) : IRequest<TransactionResponse>;
