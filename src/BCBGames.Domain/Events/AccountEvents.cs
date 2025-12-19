using System.Text.Json.Serialization;

namespace BCBGames.Domain.Events;

public class AccountCreatedEvent : DomainEvent
{
    public string AccountNumber { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }

    public AccountCreatedEvent(
        Guid accountId,
        string accountNumber,
        string ownerName,
        decimal initialBalance,
        string idempotencyKey)
        : base(accountId, idempotencyKey, version: 1)
    {
        AccountNumber = accountNumber;
        OwnerName = ownerName;
        InitialBalance = initialBalance;
    }

    [JsonConstructor]
    private AccountCreatedEvent() : base()
    {
    }
}
