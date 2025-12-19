using BCBGames.Application.DTOs;
using MediatR;

namespace BCBGames.Application.Commands.CreateAccount;

public record CreateAccountCommand(
    string OwnerName,
    decimal InitialBalance = 0,
    string? IdempotencyKey = null) : IRequest<AccountResponse>;
