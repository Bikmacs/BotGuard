using System.ComponentModel.DataAnnotations;
using BotGuard.Shared.Events;

namespace BotGuard.Shared.Models;

public class Incident
{
    public Guid Id { get; init; } = Guid.NewGuid();
    [StringLength(64, MinimumLength = 1)] public string IpAddress { get; init; } = string.Empty;
    public AbuseReason Reason { get; init; }
    [StringLength(64, MinimumLength = 1)] public string UserAgent { get; set; } = string.Empty;
    public DateTimeOffset DetectedAtUtc { get; init; }
    public DateTimeOffset HandledAtUtc { get; init; } = DateTimeOffset.UtcNow;
}