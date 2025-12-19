using System.Text;
using System.Text.Json;
using BCBGames.Domain.Events;
using BCBGames.Domain.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BCBGames.Infrastructure.EventSourcing;

public class KafkaEventStore : IEventStore, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventStore> _logger;
    private readonly string _accountEventsTopic;
    private readonly string _transactionEventsTopic;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public KafkaEventStore(
        IConfiguration configuration,
        ILogger<KafkaEventStore> logger)
    {
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Kafka:BootstrapServers configuration is required");

        _accountEventsTopic = configuration["Kafka:Topics:AccountEvents"] ?? "account-events";
        _transactionEventsTopic = configuration["Kafka:Topics:TransactionEvents"] ?? "transaction-events";

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MaxInFlight = 5,
            RetryBackoffMs = 100,
            MessageSendMaxRetries = 3
        };

        _producer = new ProducerBuilder<string, string>(producerConfig)
            .SetErrorHandler((producer, error) =>
            {
                logger.LogError("Kafka producer error: {Error}", error);
            })
            .SetLogHandler((producer, logMessage) =>
            {
                if (logMessage.Level >= SyslogLevel.Warning)
                {
                    logger.LogWarning("Kafka producer log: {Message}", logMessage.Message);
                }
            })
            .Build();

        _logger.LogInformation(
            "KafkaEventStore initialized. Topics: AccountEvents={AccountTopic}, TransactionEvents={TransactionTopic}",
            _accountEventsTopic, _transactionEventsTopic);
    }

    public async Task PublishAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        await PublishBatchAsync(new[] { @event }, cancellationToken);
    }

    public async Task PublishBatchAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default)
    {
        var eventsList = events.ToList();
        if (!eventsList.Any())
            return;

        try
        {
            var tasks = new List<Task<DeliveryResult<string, string>>>();

            foreach (var @event in eventsList)
            {
                var topic = DetermineTopic(@event);
                var key = @event.AggregateId.ToString();
                var value = SerializeEvent(@event);

                var message = new Message<string, string>
                {
                    Key = key,
                    Value = value,
                    Headers = new Headers
                    {
                        { "event-type", Encoding.UTF8.GetBytes(@event.EventType) },
                        { "idempotency-key", Encoding.UTF8.GetBytes(@event.IdempotencyKey) },
                        { "event-id", Encoding.UTF8.GetBytes(@event.Id.ToString()) }
                    }
                };

                var task = _producer.ProduceAsync(topic, message, cancellationToken);
                tasks.Add(task);

                _logger.LogDebug(
                    "Publishing event {EventType} with Id={EventId}, AggregateId={AggregateId}, IdempotencyKey={IdempotencyKey}",
                    @event.EventType, @event.Id, @event.AggregateId, @event.IdempotencyKey);
            }

            var results = await Task.WhenAll(tasks);

            _logger.LogInformation(
                "Successfully published {Count} events (idempotent producer)",
                eventsList.Count);
        }
        catch (KafkaException ex)
        {
            _logger.LogError(ex, "Error publishing events to Kafka");
            throw new InvalidOperationException("Failed to publish events to Kafka", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error publishing events");
            throw;
        }
    }

    private string DetermineTopic(DomainEvent @event)
    {
        return @event switch
        {
            AccountCreatedEvent => _accountEventsTopic,
            DepositedEvent => _transactionEventsTopic,
            WithdrawnEvent => _transactionEventsTopic,
            PurchasedEvent => _transactionEventsTopic,
            _ => _transactionEventsTopic
        };
    }

    private string SerializeEvent(DomainEvent @event)
    {
        try
        {
            return JsonSerializer.Serialize(@event, @event.GetType(), JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing event {EventType}", @event.GetType().Name);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Kafka producer");
        }
    }
}
