using BotGuard.Shared.Constants;
using StackExchange.Redis;

namespace BotGuard.Gateway.Services;

public class RedisRateLimitService(IConnectionMultiplexer redis)
{
    private const int MaxRequests = 5;
    private static readonly TimeSpan WindowDuration = TimeSpan.FromSeconds(10);
    private readonly IConnectionMultiplexer _redis;


    public async Task<bool> IsBlacklistedAsync(string ipAddress)
    {
        var cache = redis.GetDatabase();
        var key = RedisKeys.BlockList(ipAddress);

        return await cache.KeyExistsAsync(key);
    }

    public async Task<bool> TryAcquireAsync(string ipAddress)
    {
        var cache = redis.GetDatabase();
        var key = RedisKeys.RateLimiter(ipAddress);

        long currentCount = await cache.StringIncrementAsync(key);

        if (currentCount == 1)
            await cache.KeyExpireAsync(key, WindowDuration);

        return currentCount <= MaxRequests;
    }

    public async Task BanIpAsync(string ipAddress, TimeSpan duration)
    {
        var cache = redis.GetDatabase();
        var key = RedisKeys.BlockList(ipAddress);
        
        await cache.StringSetAsync(key, "banned", duration);
    }
}