using System.Text;
using System.Text.Json;
using BCBGames.Domain.Events;
using BCBGames.Domain.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BCBGames.Infrastructure.EventSourcing;

public class KafkaConsumerService : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _accountEventsTopic;
    private readonly string _transactionEventsTopic;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public KafkaConsumerService(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<KafkaConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Kafka:BootstrapServers configuration is required");

        _accountEventsTopic = configuration["Kafka:Topics:AccountEvents"] ?? "account-events";
        _transactionEventsTopic = configuration["Kafka:Topics:TransactionEvents"] ?? "transaction-events";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = "bcbgames-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnablePartitionEof = true,
            MaxPollIntervalMs = 300000,
            SessionTimeoutMs = 30000,
            HeartbeatIntervalMs = 10000
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((consumer, error) =>
            {
                logger.LogError("Kafka consumer error: {Error}", error);
            })
            .SetLogHandler((consumer, logMessage) =>
            {
                if (logMessage.Level >= SyslogLevel.Warning)
                {
                    logger.LogWarning("Kafka consumer log: {Message}", logMessage.Message);
                }
            })
            .Build();

        _logger.LogInformation(
            "KafkaConsumerService initialized as integrity monitor. Topics: AccountEvents={AccountTopic}, TransactionEvents={TransactionTopic}",
            _accountEventsTopic, _transactionEventsTopic);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = Task.Run(async () => await ConsumeEventsAsync(stoppingToken), stoppingToken);
        return Task.CompletedTask;
    }

    private async Task ConsumeEventsAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(new[] { _accountEventsTopic, _transactionEventsTopic });

        _logger.LogInformation("Kafka consumer started. Waiting for events...");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(TimeSpan.FromSeconds(1));

                    if (result == null)
                        continue;

                    if (result.IsPartitionEOF)
                    {
                        _logger.LogDebug(
                            "Reached end of partition {Partition} at offset {Offset}",
                            result.Partition, result.Offset);
                        continue;
                    }

                    await ProcessMessageAsync(result, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in Kafka consumer");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer is stopping...");
        }
        finally
        {
            _consumer.Close();
            _consumer.Dispose();
            _logger.LogInformation("Kafka consumer stopped");
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken cancellationToken)
    {
        try
        {
            var topic = result.Topic;
            var eventType = GetEventType(result.Message.Headers);
            var idempotencyKey = GetIdempotencyKey(result.Message.Headers);

            _logger.LogDebug(
                "Processing event: Topic={Topic}, EventType={EventType}, IdempotencyKey={IdempotencyKey}, Offset={Offset}",
                topic, eventType, idempotencyKey, result.Offset);

            var @event = DeserializeEvent(result.Message.Value, eventType);
            if (@event == null)
            {
                _logger.LogWarning("Could not deserialize event of type {EventType}", eventType);
                _consumer.Commit(result);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var idempotencyService = scope.ServiceProvider.GetRequiredService<IIdempotencyService>();

            if (await idempotencyService.IsDuplicateAsync(idempotencyKey, cancellationToken))
            {
                _logger.LogWarning(
                    "Duplicate event detected by idempotency key: {Key}. Skipping processing.",
                    idempotencyKey);
                _consumer.Commit(result);
                return;
            }

            await CheckEventIntegrityAsync(@event, scope, cancellationToken);

            await idempotencyService.StoreIdempotencyKeyAsync(idempotencyKey, cancellationToken: cancellationToken);

            _consumer.Commit(result);

            _logger.LogDebug(
                "Event integrity checked: EventType={EventType}, EventId={EventId}, AggregateId={AggregateId}",
                @event.EventType, @event.Id, @event.AggregateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing message at offset {Offset} from topic {Topic}",
                result.Offset, result.Topic);
            throw;
        }
    }

    private async Task CheckEventIntegrityAsync(
        DomainEvent @event,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var account = await unitOfWork.Accounts.GetByIdAsync(@event.AggregateId, cancellationToken);

        if (account == null)
        {
            _logger.LogWarning(
                "Divergence detected: Account {AccountId} not found in PostgreSQL for event {EventType} (EventId={EventId})",
                @event.AggregateId, @event.EventType, @event.Id);
            return;
        }

        var expectedBalance = GetExpectedBalanceFromEvent(@event);
        if (expectedBalance.HasValue && account.Balance != expectedBalance.Value)
        {
            _logger.LogWarning(
                "Divergence detected: Account {AccountId} has balance {ActualBalance}, expected {ExpectedBalance} for event {EventType} (EventId={EventId})",
                @event.AggregateId, account.Balance, expectedBalance.Value, @event.EventType, @event.Id);
        }
    }

    private decimal? GetExpectedBalanceFromEvent(DomainEvent @event)
    {
        return @event switch
        {
            AccountCreatedEvent e => e.InitialBalance,
            DepositedEvent e => e.BalanceAfter,
            WithdrawnEvent e => e.BalanceAfter,
            PurchasedEvent e => e.BalanceAfter,
            _ => null
        };
    }

    private string GetEventType(Headers headers)
    {
        var header = headers.FirstOrDefault(h => h.Key == "event-type");
        return header != null ? Encoding.UTF8.GetString(header.GetValueBytes()) : string.Empty;
    }

    private string GetIdempotencyKey(Headers headers)
    {
        var header = headers.FirstOrDefault(h => h.Key == "idempotency-key");
        return header != null ? Encoding.UTF8.GetString(header.GetValueBytes()) : string.Empty;
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

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}
