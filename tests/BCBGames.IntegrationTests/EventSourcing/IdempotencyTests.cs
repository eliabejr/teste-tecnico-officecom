using System.Text;
using BCBGames.Domain.Interfaces;
using BCBGames.Infrastructure.EventSourcing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BCBGames.IntegrationTests.EventSourcing;

public class IdempotencyTests
{
    [Fact]
    public void GenerateIdempotencyKey_WithoutProvidedKey_ShouldGenerateUniqueKeys()
    {
        var mockCache = new Mock<IDistributedCache>();
        var mockLogger = new Mock<ILogger<IdempotencyService>>();
        var service = new IdempotencyService(mockCache.Object, mockLogger.Object);

        var accountId = Guid.NewGuid();
        var transactionType = "DepositedEvent";
        var amount = 100.50m;

        var key1 = service.GenerateIdempotencyKey(accountId, transactionType, amount);
        var key2 = service.GenerateIdempotencyKey(accountId, transactionType, amount);

        Assert.NotEqual(key1, key2);
        Assert.Equal(64, key1.Length);
    }

    [Fact]
    public void GenerateIdempotencyKey_WithProvidedKey_ShouldBeDeterministic()
    {
        var mockCache = new Mock<IDistributedCache>();
        var mockLogger = new Mock<ILogger<IdempotencyService>>();
        var service = new IdempotencyService(mockCache.Object, mockLogger.Object);

        var accountId = Guid.NewGuid();
        var transactionType = "DepositedEvent";
        var amount = 100.50m;
        var providedKey = "client-provided-key-123";

        var key1 = service.GenerateIdempotencyKey(accountId, transactionType, amount, providedKey);
        var key2 = service.GenerateIdempotencyKey(accountId, transactionType, amount, providedKey);

        Assert.Equal(key1, key2);
        Assert.Equal(64, key1.Length);
    }

    [Fact]
    public async Task IsDuplicateAsync_WhenKeyNotExists_ShouldReturnFalse()
    {
        var mockCache = new Mock<IDistributedCache>();
        mockCache.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var mockLogger = new Mock<ILogger<IdempotencyService>>();
        var service = new IdempotencyService(mockCache.Object, mockLogger.Object);

        var result = await service.IsDuplicateAsync("test-key");

        Assert.False(result);
    }

    [Fact]
    public async Task IsDuplicateAsync_WhenKeyExists_ShouldReturnTrue()
    {
        var mockCache = new Mock<IDistributedCache>();
        var cachedData = Encoding.UTF8.GetBytes("{\"IdempotencyKey\":\"test-key\"}");
        mockCache.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedData);

        var mockLogger = new Mock<ILogger<IdempotencyService>>();
        var service = new IdempotencyService(mockCache.Object, mockLogger.Object);

        var result = await service.IsDuplicateAsync("test-key");

        Assert.True(result);
    }

    [Fact]
    public async Task StoreIdempotencyKeyAsync_WhenKeyNotExists_ShouldReturnTrue()
    {
        var mockCache = new Mock<IDistributedCache>();
        mockCache.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var mockLogger = new Mock<ILogger<IdempotencyService>>();
        var service = new IdempotencyService(mockCache.Object, mockLogger.Object);

        var result = await service.StoreIdempotencyKeyAsync("test-key");

        Assert.True(result);
        mockCache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StoreIdempotencyKeyAsync_WhenKeyExists_ShouldReturnFalse()
    {
        var mockCache = new Mock<IDistributedCache>();
        var cachedData = Encoding.UTF8.GetBytes("{\"IdempotencyKey\":\"test-key\"}");
        mockCache.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedData);

        var mockLogger = new Mock<ILogger<IdempotencyService>>();
        var service = new IdempotencyService(mockCache.Object, mockLogger.Object);

        var result = await service.StoreIdempotencyKeyAsync("test-key");

        Assert.False(result);
        mockCache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
