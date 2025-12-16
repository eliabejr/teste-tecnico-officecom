using System.ComponentModel.DataAnnotations;

namespace BCBGames.Domain.Entities;

public class Account
{
    public Guid Id { get; private set; }
    
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
    
    public static Account Create(string ownerName, decimal initialBalance = 0)
    {
        if (string.IsNullOrWhiteSpace(ownerName))
            throw new ArgumentException("Owner name is required", nameof(ownerName));
        
        if (initialBalance < 0)
            throw new ArgumentException("Initial balance cannot be negative", nameof(initialBalance));
        
        return new Account
        {
            Id = Guid.NewGuid(),
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
    
    private static string GenerateAccountNumber()
    {
        var random = new Random();
        return $"{random.Next(10000, 99999)}-{random.Next(0, 9)}";
    }
}
