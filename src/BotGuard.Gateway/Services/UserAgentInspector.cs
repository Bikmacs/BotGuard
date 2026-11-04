using System;
using System.Linq;

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
    
    public bool IsSuspicious(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return true;

        return BotSignature.Any(signature => userAgent.Contains(signature, StringComparison.OrdinalIgnoreCase));
    }
}