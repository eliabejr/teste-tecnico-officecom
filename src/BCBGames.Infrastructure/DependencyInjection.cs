using BCBGames.Domain.Interfaces;
using BCBGames.Infrastructure.Cache;
using BCBGames.Infrastructure.Data;
using BCBGames.Infrastructure.EventSourcing;
using BCBGames.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BCBGames.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: new[] { "40001", "40P01", "57014" });

                npgsqlOptions.CommandTimeout(30);
            });
        });

        var redisConnectionString = configuration.GetSection("Redis:ConnectionString").Value
            ?? configuration.GetConnectionString("Redis")
            ?? "localhost:6379";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "BCBGames:";
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configuration = ConfigurationOptions.Parse(redisConnectionString, true);
            configuration.ResolveDns = true;
            configuration.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(configuration);
        });

        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IDistributedLockService, RedisDistributedLockService>();

        services.AddSingleton<IEventStore, KafkaEventStore>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();
        services.AddScoped<IOutboxService, OutboxService>();

        services.AddHostedService<KafkaConsumerService>();
        services.AddHostedService<OutboxPublisherService>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
