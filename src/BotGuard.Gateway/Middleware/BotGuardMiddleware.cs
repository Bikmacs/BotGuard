using System.Net.Sockets;
using BotGuard.Gateway.Services;
using BotGuard.Shared.Events;
using MassTransit;

namespace BotGuard.Gateway.Middleware;

/// <summary>
/// Главный Middleware BotGuard, выполняющий роль "фейс-контроля".
/// 
/// <para>
/// Перехватывает каждый входящий HTTP-запрос и проводит две проверки:
/// 1. Анализ User-Agent на сигнатуры ботов и утилит (curl, python, headless).
/// 2. Ограничение частоты запросов (Rate Limiting) по IP-адресу.
/// </para>
/// с кодом 403 (Forbidden) или 429 (Too Many Requests), и не доходит до YARP.
/// </summary>
/// <param name="next">Делегат следующего Middleware в пайплайне (в нашем случае — YARP).</param>
/// <param name="userAgentInspector">Сервис проверки User-Agent на признаки бота.</param>
/// <param name="logger">Логгер для записи инцидентов безопасности.</param>
/// <param name="redisRateLimitService">
public class BotGuardMiddleware(
    RequestDelegate next,
    UserAgentInspector userAgentInspector,
    RedisRateLimitService redisRateLimitService,
    ILogger<BotGuardMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IPublishEndpoint publishEndpoint)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(clientIp))
        {
            logger.LogWarning("Не удалось определить IP-адрес. Блокируем запрос.");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("403 Forbidden: Suspicious client.", context.RequestAborted);
            return;
        }

        var userAgent = context.Request.Headers.UserAgent.ToString();

        if (userAgentInspector.IsSuspicious(userAgent, context.RequestAborted))
        {
            logger.LogWarning("Блокировка по User-Agent: {Ip}, UA: {UA}", clientIp, userAgent);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            try
            {
                var first = await redisRateLimitService.TryMarkReported(clientIp, "ua", TimeSpan.FromMinutes(1),
                    context.RequestAborted);
                if (first)
                {
                    var evt = new AbuseDetectedEvent(clientIp, AbuseReason.SuspiciousUserAgent, userAgent,
                        DateTimeOffset.UtcNow);
                    await SafePublishAsync(publishEndpoint, evt, logger, context.RequestAborted);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Ошибка, сбой соединения с брокером");
            }

            await context.Response.WriteAsync("403 Forbidden: Suspicious client.", context.RequestAborted);
            return;
        }

        if (await redisRateLimitService.IsBlacklistedAsync(clientIp, context.RequestAborted))
        {
            logger.LogWarning("IP в чёрном списке: {Ip}", clientIp);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            await context.Response.WriteAsync("403 Forbidden: Your IP is blacklisted.", context.RequestAborted);
            return;
        }

        if (!await redisRateLimitService.TryAcquireAsync(clientIp, context.RequestAborted))
        {
            logger.LogWarning("Превышен Rate Limit для IP: {Ip}", clientIp);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            try
            {
                var first = await redisRateLimitService.TryMarkReported(clientIp, "rateLimit", TimeSpan.FromMinutes(1),
                    context.RequestAborted);
                if (first)
                {
                    var evt = new AbuseDetectedEvent(clientIp, AbuseReason.RateLimitExceeded, userAgent,
                        DateTimeOffset.UtcNow);

                    await SafePublishAsync(publishEndpoint, evt, logger, context.RequestAborted);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Ошибка, сбой соединения с брокером");
            }


            context.Response.Headers.RetryAfter = "10";
            await context.Response.WriteAsync("429 Too Many Requests: Rate limit exceeded. Try again in 10 seconds.",
                context.RequestAborted);
            return;
        }

        await next(context);
    }

    private static async Task SafePublishAsync(
        IPublishEndpoint publishEndpoint,
        AbuseDetectedEvent evt,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            await publishEndpoint.Publish(evt, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Не удалось опубликовать AbuseDetectedEvent");
        }
    }
}