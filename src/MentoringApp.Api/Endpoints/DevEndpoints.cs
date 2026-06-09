using MentoringApp.Service;
using MentoringApp.Data.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MentoringApp.Api.Endpoints;

/// <summary>Maps the /api/dev minimal-API endpoints used only in development (e.g. recreate &amp; reseed the database). Unauthenticated for convenience.</summary>
public static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/dev");

        // Allow unauthenticated access for simplicity during dev DB recreation
        group.MapPost("/recreate-db", async (SystemAdminService adminService) =>
        {
            await adminService.RecreateDatabaseAsync();
            await adminService.SeedDatabaseAsync();
            return Results.Ok(new { message = "Database recreated and seeded successfully." });
        }).AllowAnonymous();

        return routes;
    }
}
