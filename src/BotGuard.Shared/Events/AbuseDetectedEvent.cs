using System;

namespace BotGuard.Shared.Events;

/// <summary>
/// Это карточка инцидента. Шлюз (Gateway) публикует её в RabbitMQ, а фоновый воркер (BanConsumer) достает из очереди и обрабатывает.
/// IpAddress — IP-адрес нарушителя.
/// AbuseReason Reason — типизированная причина из созданного ранее enum.
/// UserAgent — строка User-Agent.
/// DateTimeOffset DetectedAtUtc — точное время фиксации атаки.
/// </summary>
public record AbuseDetectedEvent(
        string IpAddress, 
        AbuseReason Reason, 
        string? UserAgent, 
        DateTimeOffset DetectedAtUtc);