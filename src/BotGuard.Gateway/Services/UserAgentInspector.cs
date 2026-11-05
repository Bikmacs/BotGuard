namespace BotGuard.Gateway.Services;

public class UserAgentInspector
{
    private static readonly string[] BotSignature =
    {
        "curl", "wget", "postmanruntime", "insomnia",
        "python-requests", "python-urllib", "go-http-client", "java/", "okhttp",
        "headlesschrome", "phantomjs", "selenium", "puppeteer", "playwright",
        "bot", "crawler", "spider", "scraper", "slurp"
    };
    
    private static readonly string[] AllowedBots =
    {
        "googlebot", "bingbot", "yandexbot"
    };

    public bool IsSuspicious(string? userAgent, CancellationToken  ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(userAgent))
            return true;

        var isAllowed = AllowedBots.Any(bots => userAgent.Contains(bots, StringComparison.OrdinalIgnoreCase));
        return !isAllowed && BotSignature.Any(signature => userAgent.Contains(signature, StringComparison.OrdinalIgnoreCase));
    }
}