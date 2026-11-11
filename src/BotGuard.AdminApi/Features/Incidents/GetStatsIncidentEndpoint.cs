using BotGuard.AdminApi.Features.Incidents.DTOs;
using BotGuard.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace BotGuard.AdminApi.Features.Incidents;

public static class GetStatsIncidentEndpoint
{
    public static void MapGetStatsIncidentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/incidents/stats",
            async (AppDbContext db, CancellationToken ct = default(CancellationToken)) =>
            {
                var total = await db.Incidents.CountAsync(ct);

                var topItems = await db.Incidents
                    .AsNoTracking()
                    .GroupBy(x => x.IpAddress)
                    .OrderByDescending(g => g.Count()).Take(5)
                    .Select(x =>
                        new IncidentsStats(x.Key, x.Count(),
                            x.OrderByDescending(y => y.DetectedAtUtc)
                                .Select(y => y.Reason.ToString())
                                .First()))
                    .ToListAsync(ct);

                return Results.Ok(new { Total = total, TopItems = topItems });
            });
    }
}