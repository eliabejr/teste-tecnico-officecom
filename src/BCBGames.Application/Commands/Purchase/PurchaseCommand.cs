using BCBGames.Application.DTOs;
using MediatR;

namespace BCBGames.Application.Commands.Purchase;

public record PurchaseCommand(
    Guid UserId,
    Guid AccountId,
    decimal Amount,
    string Merchant,
    string? IdempotencyKey = null) : IRequest<TransactionResponse>;
