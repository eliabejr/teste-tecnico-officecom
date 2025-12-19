using System.Text.Json;
using BCBGames.Domain.Events;
using BCBGames.Domain.Interfaces;
using BCBGames.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BCBGames.Infrastructure.EventSourcing;

public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 50;
    private readonly int _maxAttempts = 5;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public OutboxPublisherService(
        IServiceProvider serviceProvider,
        ILogger<OutboxPublisherService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisherService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxPublisherService stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        var pendingMessages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.Attempts < _maxAttempts)
            .OrderBy(m => m.OccurredAt)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
            return;

        _logger.LogDebug("Processing {Count} outbox messages", pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            try
            {
                var @event = DeserializeEvent(message.PayloadJson, message.EventType);
                if (@event == null)
                {
                    _logger.LogWarning(
                        "Could not deserialize event type {EventType} for message {MessageId}",
                        message.EventType, message.Id);
                    await MarkAsProcessedWithErrorAsync(dbContext, message, "Could not deserialize event", cancellationToken);
                    continue;
                }

                await eventStore.PublishAsync(@event, cancellationToken);

                message.ProcessedAt = DateTime.UtcNow;
                message.Attempts++;
                _logger.LogDebug(
                    "Successfully published outbox message {MessageId} (EventType={EventType}, Attempt={Attempt})",
                    message.Id, message.EventType, message.Attempts);

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;

                _logger.LogWarning(ex,
                    "Error publishing outbox message {MessageId} (Attempt={Attempt}/{MaxAttempts})",
                    message.Id, message.Attempts, _maxAttempts);

                if (message.Attempts >= _maxAttempts)
                {
                    _logger.LogError(
                        "Outbox message {MessageId} exceeded max attempts ({MaxAttempts}). Manual intervention required.",
                        message.Id, _maxAttempts);
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                if (message.Attempts < _maxAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
        }
    }

    private async Task MarkAsProcessedWithErrorAsync(
        AppDbContext dbContext,
        OutboxMessage message,
        string error,
        CancellationToken cancellationToken)
    {
        message.ProcessedAt = DateTime.UtcNow;
        message.Attempts = _maxAttempts;
        message.LastError = error;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private DomainEvent? DeserializeEvent(string json, string eventType)
    {
        try
        {
            return eventType switch
            {
                nameof(AccountCreatedEvent) => JsonSerializer.Deserialize<AccountCreatedEvent>(json, JsonOptions),
                nameof(DepositedEvent) => JsonSerializer.Deserialize<DepositedEvent>(json, JsonOptions),
                nameof(WithdrawnEvent) => JsonSerializer.Deserialize<WithdrawnEvent>(json, JsonOptions),
                nameof(PurchasedEvent) => JsonSerializer.Deserialize<PurchasedEvent>(json, JsonOptions),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing event of type {EventType}", eventType);
            return null;
        }
    }
}
