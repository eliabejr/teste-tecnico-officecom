using System.ComponentModel.DataAnnotations;
using BCBGames.Domain.Enums;

namespace BCBGames.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    
    public Guid AccountId { get; private set; }
    
    public TransactionType Type { get; private set; }
    
    public decimal Amount { get; private set; }
    
    [MaxLength(500)]
    public string? Description { get; private set; }
    
    public decimal BalanceBefore { get; private set; }
    
    public decimal BalanceAfter { get; private set; }
    
    public TransactionStatus Status { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    
    public DateTime? ProcessedAt { get; private set; }
    
    public Account? Account { get; private set; }
    
    private Transaction() { }
    
    public static Transaction Create(
        Guid accountId,
        TransactionType type,
        decimal amount,
        decimal balanceBefore,
        string? description = null)
    {
        if (amount < 0.01m)
            throw new ArgumentException("Minimum amount is R$ 0.01", nameof(amount));
        
        return new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Type = type,
            Amount = amount,
            Description = description?.Trim(),
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceBefore,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public void Complete(decimal balanceAfter)
    {
        BalanceAfter = balanceAfter;
        Status = TransactionStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
    }
    
    public void Fail()
    {
        Status = TransactionStatus.Failed;
        ProcessedAt = DateTime.UtcNow;
    }
}
