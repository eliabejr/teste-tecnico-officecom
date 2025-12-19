using System.Text.Json.Serialization;

namespace BCBGames.Domain.Events;

public class DepositedEvent : DomainEvent
{
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }

    public DepositedEvent(
        Guid accountId,
        Guid transactionId,
        decimal amount,
        decimal balanceBefore,
        decimal balanceAfter,
        string idempotencyKey,
        string? description = null,
        int version = 1)
        : base(accountId, idempotencyKey, version)
    {
        TransactionId = transactionId;
        Amount = amount;
        Description = description;
        BalanceBefore = balanceBefore;
        BalanceAfter = balanceAfter;
    }

    [JsonConstructor]
    private DepositedEvent() : base()
    {
    }
}

public class WithdrawnEvent : DomainEvent
{
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }

    public WithdrawnEvent(
        Guid accountId,
        Guid transactionId,
        decimal amount,
        decimal balanceBefore,
        decimal balanceAfter,
        string idempotencyKey,
        string? description = null,
        int version = 1)
        : base(accountId, idempotencyKey, version)
    {
        TransactionId = transactionId;
        Amount = amount;
        Description = description;
        BalanceBefore = balanceBefore;
        BalanceAfter = balanceAfter;
    }

    [JsonConstructor]
    private WithdrawnEvent() : base()
    {
    }
}

public class PurchasedEvent : DomainEvent
{
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Merchant { get; set; } = string.Empty;
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }

    public PurchasedEvent(
        Guid accountId,
        Guid transactionId,
        decimal amount,
        string merchant,
        decimal balanceBefore,
        decimal balanceAfter,
        string idempotencyKey,
        int version = 1)
        : base(accountId, idempotencyKey, version)
    {
        TransactionId = transactionId;
        Amount = amount;
        Merchant = merchant;
        BalanceBefore = balanceBefore;
        BalanceAfter = balanceAfter;
    }

    [JsonConstructor]
    private PurchasedEvent() : base()
    {
    }
}
