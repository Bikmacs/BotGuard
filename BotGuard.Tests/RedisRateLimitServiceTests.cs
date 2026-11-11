using BotGuard.Gateway.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;

namespace BotGuard.Tests;

public class RedisRateLimitServiceTests
{
    [Theory]
    [InlineData("1.2.3.4", true)]
    [InlineData("5.6.7.8", false)]
    public async Task IsBlacklistedAsync_ReturnsExpectedResult(string ipAddress, bool expectedResult)
    {
        IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

        var mockStock = new Mock<IDatabase>();
        mockStock.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(expectedResult);

        var mockMultiplexer = new Mock<IConnectionMultiplexer>();

        mockMultiplexer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockStock.Object);
        var service = new RedisRateLimitService(mockMultiplexer.Object, NullLogger<RedisRateLimitService>.Instance, memoryCache);

        var actual = await service.IsBlacklistedAsync(ipAddress, CancellationToken.None);
        Assert.Equal(expectedResult, actual);
    }

    [Fact]
    public async Task IsFailOpen_ReturnsExpectedResult()
    {
        IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        
        var mockMultiplexer = new Mock<IConnectionMultiplexer>();

        mockMultiplexer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Throws(new RedisException("Redis is down"));

        var service = new RedisRateLimitService(mockMultiplexer.Object, NullLogger<RedisRateLimitService>.Instance, memoryCache );

        var resultBlackList = await service.IsBlacklistedAsync("1.2.3.4", CancellationToken.None);
        var resultTryAcquireAsync = await service.TryAcquireAsync("1.2.3.4", CancellationToken.None);
        var resultTryMarkReported =
            await service.TryMarkReported("1.2.3.4", reason: "test-reason", TimeSpan.FromMinutes(1),
                CancellationToken.None);

        Assert.False(resultBlackList); // Не в бане (пропускаем)
        Assert.True(resultTryAcquireAsync); // Лимит не превышен (пропускаем)
        Assert.False(resultTryMarkReported); // Не смогли поставить маркер (чтобы не спамит
    }

    [Theory]
    [InlineData(1L, true)]
    [InlineData(5L, true)]
    [InlineData(6L, false)]
    public async Task IsTryAcquireAsync_ReturnsExpectedResult(long countFromRedis, bool expectedResult)
    {
        IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

        var mockStock = new Mock<IDatabase>();
        mockStock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>())).ReturnsAsync(RedisResult.Create(countFromRedis));

        var mockMultiplexer = new Mock<IConnectionMultiplexer>();
        mockMultiplexer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockStock.Object);

        var service = new RedisRateLimitService(mockMultiplexer.Object, NullLogger<RedisRateLimitService>.Instance, memoryCache);

        var actualResult = await service.TryAcquireAsync("1.2.3.4", CancellationToken.None);        
        Assert.Equal(expectedResult, actualResult );
    }
}