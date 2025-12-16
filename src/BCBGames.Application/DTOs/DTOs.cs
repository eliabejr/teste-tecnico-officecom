using System.ComponentModel.DataAnnotations;
using BCBGames.Domain.Enums;

namespace BCBGames.Application.DTOs;

public record CreateAccountRequest(
    [Required, MinLength(2), MaxLength(200)] string OwnerName,
    [Range(0, double.MaxValue)] decimal InitialBalance = 0);

public record AccountResponse(
    Guid Id,
    string AccountNumber,
    string OwnerName,
    decimal Balance,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record BalanceResponse(
    Guid AccountId,
    string AccountNumber,
    decimal Balance,
    DateTime AsOf);

public record DepositRequest(
    [Required] Guid AccountId,
    [Range(0.01, double.MaxValue, ErrorMessage = "minimum amount is R$ 0.01")] decimal Amount,
    [MaxLength(500)] string? Description = null);

public record WithdrawRequest(
    [Required] Guid AccountId,
    [Range(0.01, double.MaxValue, ErrorMessage = "minimum amount is R$ 0.01")] decimal Amount,
    [MaxLength(500)] string? Description = null);

public record PurchaseRequest(
    [Required] Guid AccountId,
    [Range(0.01, double.MaxValue, ErrorMessage = "minimum amount is R$ 0.01")] decimal Amount,
    [Required, MinLength(1), MaxLength(500)] string Merchant);

public record TransactionResponse(
    Guid Id,
    Guid AccountId,
    TransactionType Type,
    decimal Amount,
    string? Description,
    decimal BalanceBefore,
    decimal BalanceAfter,
    TransactionStatus Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt);

public record StatementResponse(
    Guid AccountId,
    string AccountNumber,
    decimal CurrentBalance,
    int TotalTransactions,
    int Page,
    int PageSize,
    IEnumerable<TransactionResponse> Transactions);

public record ErrorResponse(
    string Message,
    string? Detail = null,
    int StatusCode = 400);
