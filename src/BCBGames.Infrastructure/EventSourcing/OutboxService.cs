using System.Text.Json;
using BCBGames.Domain.Events;
using BCBGames.Domain.Interfaces;
using BCBGames.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BCBGames.Infrastructure.EventSourcing;

public class OutboxService : IOutboxService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OutboxService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public OutboxService(AppDbContext dbContext, ILogger<OutboxService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task AddAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = JsonSerializer.Serialize(@event, @event.GetType(), JsonOptions);

            var outboxMessage = new OutboxMessage
            {
                Id = @event.Id,
                OccurredAt = @event.Timestamp,
                EventType = @event.EventType,
                AggregateId = @event.AggregateId,
                IdempotencyKey = @event.IdempotencyKey,
                PayloadJson = payload,
                Attempts = 0
            };

            await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);

            _logger.LogDebug(
                "Added event to outbox: EventType={EventType}, EventId={EventId}, AggregateId={AggregateId}, IdempotencyKey={IdempotencyKey}",
                @event.EventType, @event.Id, @event.AggregateId, @event.IdempotencyKey);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("unique") == true ||
                                            ex.InnerException?.Message?.Contains("duplicate") == true)
        {
            _logger.LogWarning(
                "Outbox message with IdempotencyKey={IdempotencyKey} already exists. This is expected for idempotent retries.",
                @event.IdempotencyKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error adding event to outbox: EventType={EventType}, EventId={EventId}",
                @event.EventType, @event.Id);
            throw;
        }
    }
}
