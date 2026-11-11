using BotGuard.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace BotGuard.AdminApi.Features.Incidents;

public static class RemoveIncidentsEndpoint
{
    public static void MapRemoveIncidentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("api/incidents/remove/{id:guid}", async (AppDbContext dbContext, Guid id,
            CancellationToken ct = default) =>
        {
            var incident = await dbContext.Incidents.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (incident == null)
                return Results.NotFound();

            dbContext.Incidents.Remove(incident);
            await dbContext.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}