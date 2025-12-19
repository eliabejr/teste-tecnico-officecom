using System.Text.Json.Serialization;

namespace BCBGames.Domain.Events;

public abstract class DomainEvent
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid AggregateId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int Version { get; set; }

    protected DomainEvent(Guid aggregateId, string idempotencyKey, int version = 1)
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        AggregateId = aggregateId;
        IdempotencyKey = idempotencyKey;
        EventType = GetType().Name;
        Version = version;
    }

    [JsonConstructor]
    protected DomainEvent()
    {
    }
}
