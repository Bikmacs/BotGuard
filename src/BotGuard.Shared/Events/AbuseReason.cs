namespace BotGuard.Shared.Events;

public enum AbuseReason
{
    /// <summary>
    /// Подозрительный User-Agent (curl, python-requests, headless-браузер).
    /// </summary>
    SuspiciousUserAgent = 1,

    /// <summary>
    /// Превышен лимит запросов за единицу времени (Rate Limit).
    /// </summary>
    RateLimitExceeded = 2,
    
    HoneypotTriggered = 3
}