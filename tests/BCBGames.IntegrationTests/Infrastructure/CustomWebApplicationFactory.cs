using System.Collections.Generic;
using BCBGames.Domain.Events;
using BCBGames.Domain.Interfaces;
using BCBGames.Infrastructure.Data;
using BCBGames.Infrastructure.EventSourcing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BCBGames.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public CustomWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["RateLimiting:Enabled"] = "false",
                ["Redis:ConnectionString"] = "localhost:0"
            });
        });

        builder.ConfigureServices(services =>
        {
            var kafkaConsumerHostedService = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(KafkaConsumerService));
            if (kafkaConsumerHostedService is not null)
            {
                services.Remove(kafkaConsumerHostedService);
            }

            var dbContextOptions = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (dbContextOptions is not null)
                services.Remove(dbContextOptions);

            var dbContext = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
            if (dbContext is not null)
                services.Remove(dbContext);

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: new[] { "40001", "40P01", "57014" });

                    npgsqlOptions.CommandTimeout(30);
                });
            });

            var eventStoreDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventStore));
            if (eventStoreDescriptor != null)
            {
                services.Remove(eventStoreDescriptor);
            }

            services.AddSingleton<IEventStore, MockEventStore>();

            // Replace Redis-based distributed locks with an in-memory implementation for tests.
            var lockServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDistributedLockService));
            if (lockServiceDescriptor is not null)
            {
                services.Remove(lockServiceDescriptor);
            }
            services.AddSingleton<IDistributedLockService, InMemoryDistributedLockService>();

            // Replace Redis-based cache with a no-op cache to avoid timeouts under high concurrency.
            var cacheServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICacheService));
            if (cacheServiceDescriptor is not null)
            {
                services.Remove(cacheServiceDescriptor);
            }
            services.AddSingleton<ICacheService, NoOpCacheService>();
        });
    }
}
internal class MockEventStore : IEventStore
{
    private readonly ILogger<MockEventStore> _logger;

    public MockEventStore(ILogger<MockEventStore> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("publishing event {EventType} with Id={EventId}, AggregateId={AggregateId}",
            @event.EventType, @event.Id, @event.AggregateId);
        return Task.CompletedTask;
    }

    public Task PublishBatchAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default)
    {
        var eventsList = events.ToList();
        _logger.LogDebug("publishing {Count} events", eventsList.Count);
        return Task.CompletedTask;
    }
}
