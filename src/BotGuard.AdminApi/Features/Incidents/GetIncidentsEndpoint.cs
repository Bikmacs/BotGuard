using BotGuard.AdminApi.Features.Incidents.DTOs;
using BotGuard.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace BotGuard.AdminApi.Features.Incidents;

public static class GetIncidentsEndpoint
{
    public static void MapGetIncidentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/incidents",
            async (AppDbContext db, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
            {
                var total = await db.Incidents.CountAsync(ct);

                var item = await db.Incidents
                    .AsNoTracking()
                    .OrderByDescending(c => c.DetectedAtUtc)
                    .Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(x => new IncidentDto(x.Id, x.IpAddress, x.Reason.ToString(), x.UserAgent, x.DetectedAtUtc))
                    .ToListAsync(cancellationToken: ct);

                return Results.Ok(new { Total = total, Items = item });
            });
    }
}