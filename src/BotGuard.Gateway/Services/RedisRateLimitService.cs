using BotGuard.Shared.Constants;
using StackExchange.Redis;

namespace BotGuard.Gateway.Services;

/// <summary>
/// Распределённый Rate Limiter и чёрный список на Redis.
///
/// <para>
/// <b>Почему Redis, а не память?</b> InMemory-лимиты живут в одном процессе.
/// Как только Gateway'ев становится два (или он перезапускается) — у каждого своя
/// копия счётчиков, и реальный лимит умножается на число инстансов. Redis —
/// единая точка правды: все реплики шлюза работают с одним ключом ratelimit:{ip}.
/// </para>
///
/// <para>
/// <b>Почему Lua-скрипт?</b> Redis выполняет команды по одной, но скрипт — как одна
/// команда: пока он выполняется, НИКТО другой не может вклиниться между INCR и PEXPIRE.
/// Старая версия делала два отдельных вызова: если процесс умирал между ними, ключ
/// оставался без TTL и жил вечно — IP был бы заблокирован навсегда. Условие
/// PTTL &lt; 0 дополнительно "лечит" ключи, оставшиеся без TTL с прошлых версий.
/// </para>
/// </summary>
public class RedisRateLimitService(IConnectionMultiplexer redis, ILogger<RedisRateLimitService> logger)
{
    private const int MaxRequests = 5;
    private static readonly TimeSpan WindowDuration = TimeSpan.FromSeconds(10);

    private const string FixedWindowScript = """
                                             local count = redis.call('INCR', KEYS[1])
                                             if count == 1 or redis.call('PTTL', KEYS[1]) < 0 then
                                                 redis.call('PEXPIRE', KEYS[1], ARGV[1]) 
                                             end
                                             return count
                                             """;

    public async Task<bool> IsBlacklistedAsync(string ipAddress, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var cache = redis.GetDatabase();

            return await cache.KeyExistsAsync(RedisKeys.BlackList(ipAddress));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to check blacklist");
            return false;
        }
    }

    /// <summary>
    /// true — разрешение выдано (запрос проходит), false — окно исчерпано.
    /// Семантика совпадает с InMemoryRateLimitService.TryAcquire.
    /// </summary>
    public async Task<bool> TryAcquireAsync(string ipAddress, CancellationToken ct)
    {
        try
        {
            var cache = redis.GetDatabase();

            var count = (long)await cache.ScriptEvaluateAsync(
                FixedWindowScript,
                [RedisKeys.RateLimiter(ipAddress)],
                [(long)WindowDuration.TotalMilliseconds]).WaitAsync(ct);

            return count <= MaxRequests;
        }
        catch (Exception ex) when (ex is OperationCanceledException)
        {
            logger.LogError(ex, "Failed to acquire rate limit");
            return true;
        }
    }

    public async Task<bool> TryMarkReported(string ipAddress, string reason, TimeSpan duration, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var cache = redis.GetDatabase();
            var key = $"reported:{reason}:{ipAddress}";
            return await cache.StringSetAsync(key, "1", duration, When.NotExists);
        }
        catch (Exception ex) when (ex is OperationCanceledException)
        {
            logger.LogError(ex, "Failed to mark IP as reported");
            return false;
        }
    }
}