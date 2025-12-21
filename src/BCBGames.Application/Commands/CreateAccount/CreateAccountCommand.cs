using BCBGames.Application.DTOs;
using MediatR;

namespace BCBGames.Application.Commands.CreateAccount;

public record CreateAccountCommand(
    Guid UserId,
    string OwnerName,
    decimal InitialBalance = 0,
    string? IdempotencyKey = null) : IRequest<AccountResponse>;
