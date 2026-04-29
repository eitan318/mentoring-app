using MentoringApp.Api.Helpers;
using MentoringApp.Data.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MentoringApp.Api.Endpoints;

public static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/dev");

        // Allow unauthenticated access for simplicity during dev DB recreation
        group.MapPost("/recreate-db", async (IDbRepo dbRepo, DummyDataSeeder seeder) =>
        {
            dbRepo.Recreate();
            await seeder.SeedAsync();
            return Results.Ok(new { message = "Database recreated and seeded successfully." });
        }).AllowAnonymous();

        return routes;
    }
}
