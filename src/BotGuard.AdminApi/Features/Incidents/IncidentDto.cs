using System.ComponentModel.DataAnnotations;
using BotGuard.Shared.Events;

namespace BotGuard.AdminApi.Features.Incidents;

public record class IncidentDto(
    Guid Id,
    string ipAddress,
    string reason,
    string UserAgent,
    DateTimeOffset DetectedAt
);