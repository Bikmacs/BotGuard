using BotGuard.Shared.Constants;
using BotGuard.Shared.Data;
using BotGuard.Shared.Events;
using BotGuard.Shared.Models;
using MassTransit;
using StackExchange.Redis;

namespace BotGuard.BanConsumer;

public class AbuseDetectedConsumer(
    ILogger<AbuseDetectedConsumer> logger,
    IConnectionMultiplexer redis,
    IServiceScopeFactory scopeFactory)
    : IConsumer<AbuseDetectedEvent>
{
    public async Task Consume(ConsumeContext<AbuseDetectedEvent> context)
    {
        var evt = context.Message;
        var ct = context.CancellationToken;
        
        var safeIpAddress = string.IsNullOrEmpty(evt.IpAddress)
            ? "unknown" : (evt.IpAddress.Length > 64 ? evt.IpAddress.Substring(0, 64) : evt.IpAddress);
        
        var safeUserAgent = string.IsNullOrEmpty(evt.UserAgent) 
            ? "unknown" 
            : (evt.UserAgent.Length > 64 ? evt.UserAgent.Substring(0, 64) : evt.UserAgent);

        logger.LogWarning("Warning IP: {Ip}, Причина: {Reason}, UA: {UserAgent}",
            evt.IpAddress, evt.Reason, evt.UserAgent);

        var db = redis.GetDatabase();

        var key = RedisKeys.BlackList(evt.IpAddress);
        await db.StringSetAsync(key, "banned", TimeSpan.FromMinutes(10)).WaitAsync(ct);

        logger.LogInformation("IP {Ip} успешно забанен в Redis на 10 минут", evt.IpAddress);

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var incident = new Incident
        {
            IpAddress = safeIpAddress,
            Reason = evt.Reason,
            UserAgent = safeUserAgent,
            DetectedAtUtc = evt.DetectedAtUtc,
        };

        dbContext.Incidents.Add(incident);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Инцидент сохранен в PostgreSQL. Id: {Id}", incident.Id);
    }
}