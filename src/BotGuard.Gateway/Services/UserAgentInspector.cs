using System.Buffers;

namespace BotGuard.Gateway.Services;

public class UserAgentInspector
{
    private static readonly string[] BotSignature =
    [
        "curl", "wget", "postmanruntime", "insomnia", "httpie",
        "python-requests", "python-urllib", "python-httpx",
        "go-http-client", "java/", "okhttp", "apache-httpclient",
        "node-fetch", "axios/", "dart", "guzzle", "libwww-perl", "ruby", "php/",
        "headlesschrome", "phantomjs", "selenium", "puppeteer", "playwright",
        "scrapy", "mechanize", "beautifulsoup", "htmlunit", "slimerjs", "jsdom",
        "ahrefsbot", "semrushbot", "mj12bot", "dotbot", "screaming frog",
        "httrack", "webcopier", "flashget", "lftp",
        "uptime", "pingdom", "statuscake", "monitis", "site24x7",
        "newrelicpinger", "datadog", "zabbix", "nagios", "check_http",
        "bot", "crawler", "spider", "scraper", "archiver", "spam",
        "emailcollector", "harvest", "extractor", "validator", "nikto", "nmap",
        "sqlmap", "masscan", "zgrab"
    ];

    private static readonly string[] AllowedBots =
    [
        "googlebot", "mediapartners-google", "adsbot-google",
        "bingbot", "yandexbot", "yandex.com/bots", "baiduspider", "duckduckbot", "slurp", "sogou", "exabot",
        "facebookexternalhit", "twitterbot", "linkedinbot", "pinterest", "telegrambot", "whatsapp", "discordbot",
        "slackbot",
        "applebot", "uptimerobot"
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