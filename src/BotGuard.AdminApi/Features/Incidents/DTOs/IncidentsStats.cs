namespace BotGuard.AdminApi.Features.Incidents.DTOs;

public record class IncidentsStats(
    string IpAddress,
    int Count,
    string LastReason
);