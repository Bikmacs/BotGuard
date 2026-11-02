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

    /// <summary>
    /// Клиент не прошел проверку JS Proof-of-Work (не смог решить задачу).
    /// </summary>
    ProofOfWorkFailed = 3,

    /// <summary>
    /// IP находится в черном списке (был забанен ранее).
    /// </summary>
    Blacklisted = 4
}