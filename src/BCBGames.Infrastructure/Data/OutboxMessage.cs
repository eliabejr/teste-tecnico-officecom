namespace BCBGames.Infrastructure.Data;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
