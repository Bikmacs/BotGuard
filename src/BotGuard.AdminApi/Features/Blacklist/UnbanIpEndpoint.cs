using BotGuard.Shared.Constants;
using StackExchange.Redis;

namespace BotGuard.AdminApi.Features.Blacklist;

public static class UnbanIpEndpoint
{
    public static void MapUnbanIpEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/api/blacklist/{ipAddress}",
            async (IConnectionMultiplexer connection,
                string ipAddress) =>
            {
                var key = RedisKeys.BlackList(ipAddress);

                bool wasDeleted = await connection.GetDatabase().KeyDeleteAsync(key);
                if (wasDeleted)
                    return Results.Ok(new { Message = "IP unbanned" });

                return Results.NotFound(new { Message = "IP was not in blacklist" });
            });
    }
}