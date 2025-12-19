using BCBGames.Application.DTOs;
using MediatR;

namespace BCBGames.Application.Commands.Withdraw;

public record WithdrawCommand(
    Guid AccountId,
    decimal Amount,
    string? Description = null,
    string? IdempotencyKey = null) : IRequest<TransactionResponse>;
