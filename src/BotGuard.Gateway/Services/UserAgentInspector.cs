using System.Buffers;

namespace BotGuard.Gateway.Services;

public class UserAgentInspector
{
    private static readonly string[] BotSignature =
    [
        "curl", "wget", "postmanruntime", "insomnia",
        "python-requests", "python-urllib", "go-http-client", "java/", "okhttp",
        "headlesschrome", "phantomjs", "selenium", "puppeteer", "playwright",
        "bot", "crawler", "spider", "scraper", "slurp"
    ];

    private static readonly string[] AllowedBots =
    [
        "googlebot", "bingbot", "yandexbot"
    ];

    private static readonly SearchValues<string> BotSignatureValue =
        SearchValues.Create(BotSignature, StringComparison.OrdinalIgnoreCase);

    private static readonly SearchValues<string> AllowdBotsValues =
        SearchValues.Create(AllowedBots, StringComparison.OrdinalIgnoreCase);

    public bool IsSuspicious(string? userAgent, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(userAgent))
            return true;

        ReadOnlySpan<char> userAgentSpan = userAgent.AsSpan();
        return !userAgentSpan.ContainsAny(AllowdBotsValues) && userAgentSpan.ContainsAny(BotSignatureValue);
    }
}