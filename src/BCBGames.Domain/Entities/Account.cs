using System.ComponentModel.DataAnnotations;
using BCBGames.Domain.Enums;
using BCBGames.Domain.Events;

namespace BCBGames.Domain.Entities;

public class Account
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public User? User { get; private set; }

    [MaxLength(20)]
    public string AccountNumber { get; private set; } = string.Empty;

    [MaxLength(200)]
    public string OwnerName { get; private set; } = string.Empty;

    public decimal Balance { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    [ConcurrencyCheck]
    public int Version { get; private set; }

    public ICollection<Transaction> Transactions { get; private set; } = new List<Transaction>();

    private Account() { }

    public static Account Create(Guid userId, string ownerName, decimal initialBalance = 0)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));

        if (string.IsNullOrWhiteSpace(ownerName))
            throw new ArgumentException("Owner name is required", nameof(ownerName));

        if (initialBalance < 0)
            throw new ArgumentException("Initial balance cannot be negative", nameof(initialBalance));

        return new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountNumber = GenerateAccountNumber(),
            OwnerName = ownerName.Trim(),
            Balance = initialBalance,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    public void Credit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void Debit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (Balance < amount)
            throw new InvalidOperationException("Insufficient balance");

        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public bool HasSufficientBalance(decimal amount) => Balance >= amount;

    public (Transaction Transaction, DepositedEvent Event) Deposit(
        decimal amount,
        string? description,
        string idempotencyKey)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (amount < 0.01m)
            throw new ArgumentException("Minimum amount is R$ 0.01", nameof(amount));

        var balanceBefore = Balance;
        var balanceAfter = Balance + amount;

        var transaction = Transaction.Create(
            Id,
            TransactionType.Deposit,
            amount,
            balanceBefore,
            description ?? "Depósito");

        var @event = new DepositedEvent(
            Id,
            transaction.Id,
            amount,
            balanceBefore,
            balanceAfter,
            idempotencyKey,
            description ?? "Depósito");

        Balance = balanceAfter;
        Version++;
        UpdatedAt = @event.Timestamp;

        transaction.Complete(balanceAfter);

        return (transaction, @event);
    }

    public (Transaction Transaction, WithdrawnEvent Event) Withdraw(
        decimal amount,
        string? description,
        string idempotencyKey)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (amount < 0.01m)
            throw new ArgumentException("Minimum amount is R$ 0.01", nameof(amount));

        if (Balance < amount)
            throw new InvalidOperationException("Insufficient balance");

        var balanceBefore = Balance;
        var balanceAfter = Balance - amount;

        var transaction = Transaction.Create(
            Id,
            TransactionType.Withdraw,
            amount,
            balanceBefore,
            description ?? "Saque");

        var @event = new WithdrawnEvent(
            Id,
            transaction.Id,
            amount,
            balanceBefore,
            balanceAfter,
            idempotencyKey,
            description ?? "Saque");

        Balance = balanceAfter;
        Version++;
        UpdatedAt = @event.Timestamp;

        transaction.Complete(balanceAfter);

        return (transaction, @event);
    }

    public (Transaction Transaction, PurchasedEvent Event) Purchase(
        decimal amount,
        string merchant,
        string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(merchant))
            throw new ArgumentException("Merchant is required", nameof(merchant));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (amount < 0.01m)
            throw new ArgumentException("Minimum amount is R$ 0.01", nameof(amount));

        if (Balance < amount)
            throw new InvalidOperationException("Insufficient balance");

        var balanceBefore = Balance;
        var balanceAfter = Balance - amount;

        var transaction = Transaction.Create(
            Id,
            TransactionType.Purchase,
            amount,
            balanceBefore,
            $"Compra: {merchant}");

        var @event = new PurchasedEvent(
            Id,
            transaction.Id,
            amount,
            merchant,
            balanceBefore,
            balanceAfter,
            idempotencyKey);

        Balance = balanceAfter;
        Version++;
        UpdatedAt = @event.Timestamp;

        transaction.Complete(balanceAfter);

        return (transaction, @event);
    }

    public void Apply(DomainEvent @event)
    {
        switch (@event)
        {
            case AccountCreatedEvent e:
                break;

            case DepositedEvent e:
                Balance = e.BalanceAfter;
                break;

            case WithdrawnEvent e:
                Balance = e.BalanceAfter;
                break;

            case PurchasedEvent e:
                Balance = e.BalanceAfter;
                break;
        }

        Version++;
        UpdatedAt = @event.Timestamp;
    }

    private static string GenerateAccountNumber()
    {
        var random = new Random();
        return $"{random.Next(10000, 99999)}-{random.Next(0, 9)}";
    }
}
