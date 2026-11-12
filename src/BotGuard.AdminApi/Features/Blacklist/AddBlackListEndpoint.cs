using BotGuard.AdminApi.Features.Blacklist.DTOs;
using BotGuard.Shared.Constants;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace BotGuard.AdminApi.Features.Blacklist;

public static class AddBlackListEndpoint
{
    public static void MapAddBlackListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/blacklist/add",
            async ([FromBody] AddBlackListDto request,
                IConnectionMultiplexer connection,
                CancellationToken ct = default) =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(request.IpAddress);
                var key = RedisKeys.BlackList(request.IpAddress);
                await connection.GetDatabase().StringSetAsync(key, "banned", TimeSpan.FromMinutes(request.Minutes));
                return Results.Ok();
            });
    }
}