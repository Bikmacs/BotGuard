using BotGuard.Gateway.Services;

namespace BotGuard.Gateway.Middleware;
/// <summary>
/// Главный Middleware BotGuard, выполняющий роль "фейс-контроля".
/// 
/// <para>
/// Перехватывает каждый входящий HTTP-запрос и проводит две проверки:
/// 1. Анализ User-Agent на сигнатуры ботов и утилит (curl, python, headless).
/// 2. Ограничение частоты запросов (Rate Limiting) по IP-адресу.
/// </para>
/// 
/// <para>
/// Если хотя бы одна проверка не пройдена, запрос обрывается (short-circuiting)
/// с кодом 403 (Forbidden) или 429 (Too Many Requests), и не доходит до YARP.
/// </para>
/// </summary>
/// <param name="next">Делегат следующего Middleware в пайплайне (в нашем случае — YARP).</param>
/// <param name="userAgentInspector">Сервис проверки User-Agent на признаки бота.</param>
/// <param name="inMemoryRateLimitService">Сервис ограничения частоты запросов в памяти.</param>
/// <param name="logger">Логгер для записи инцидентов безопасности.</param>
public class BotGuardMiddleware(
    RequestDelegate next,
    UserAgentInspector userAgentInspector,
    InMemoryRateLimitService inMemoryRateLimitService,
    ILogger<BotGuardMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers.UserAgent.ToString();

        if (userAgentInspector.IsSuspicious(userAgent))
        {
            logger.LogWarning("Блокировка по User-Agent: {Ip}, UA: {UA}", clientIp, userAgent);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            await context.Response.WriteAsync("403 Forbidden: Suspicious client.", context.RequestAborted);
            return;
        }

        if (!inMemoryRateLimitService.TryAcquire(clientIp))
        {
            logger.LogWarning("Превышен Rate Limit для IP: {Ip}", clientIp);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            context.Response.Headers.RetryAfter = "10";
            await context.Response.WriteAsync("429 Too Many Requests: Rate limit exceeded. Try again in 10 seconds.", context.RequestAborted);
            return;
        }
        
        await next(context);
    }
}