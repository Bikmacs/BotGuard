namespace BotGuard.Shared.Constants;

/// <summary>
/// Зачем нужно: Gateway будет проверять ключи в Redis,
/// а BanConsumer будет их туда записывать. Если шаблон ключа захардкодить строкой в двух проектах,
/// любая опечатка (например, blacklist: вместо black_list:) приведет к тому, что шлюз не увидит забаненных ботов.
/// </summary>
public static class RedisKeys
{
    public static string BlackList(string ip) => $"blacklist:{ip}";
    public static string RateLimiter(string ip) => $"ratelimit:{ip}";
}