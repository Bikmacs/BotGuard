namespace BotGuard.AdminApi.Features.Incidents.DTOs;

public record class IncidentDto(
    Guid Id,
    string ipAddress,
    string reason,
    string UserAgent,
    DateTimeOffset DetectedAt
);