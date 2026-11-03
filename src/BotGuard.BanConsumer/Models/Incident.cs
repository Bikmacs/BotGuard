using BotGuard.Shared.Events;

namespace BotGuard.BanConsumer.Models;

public class Incident
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IpAddress { get; set; } = string.Empty;
    public AbuseReason Reason { get; set; }
    public string? UserAgent { get; set; } = string.Empty;
    public DateTimeOffset DetectedAtUtc { get; set; }
    public DateTimeOffset HandledAtUtc { get; set; } = DateTimeOffset.UtcNow;
}