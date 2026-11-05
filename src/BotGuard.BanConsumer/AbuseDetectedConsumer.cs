using BotGuard.BanConsumer.Data;
using BotGuard.BanConsumer.Models;
using BotGuard.Shared.Constants;
using BotGuard.Shared.Events;
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
        
        logger.LogWarning("Warning IP: {Ip}, Причина: {Reason}, UA: {UserAgent}",
            evt.IpAddress, evt.Reason, evt.UserAgent);

        var db = redis.GetDatabase();
        
        var key = RedisKeys.BlackList(evt.IpAddress);
        await db.StringSetAsync(key, "banned", TimeSpan.FromMinutes(10)).WaitAsync(ct);

        logger.LogInformation("IP {Ip} успешно забанен в Redis на 10 минут.", evt.IpAddress);

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var incident = new Incident
        {
            IpAddress = evt.IpAddress,
            Reason = evt.Reason,
            UserAgent = evt.UserAgent,
            DetectedAtUtc = evt.DetectedAtUtc,
        };

        dbContext.Incidents.Add(incident);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Инцидент сохранен в PostgreSQL. Id: {Id}", incident.Id);
    }
}