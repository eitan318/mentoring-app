using MentoringApp.Api.Helpers;
using MentoringApp.Data.Interfaces;
using MentoringApp.Service;

namespace MentoringApp.Api.Endpoints;

public static class PairEndpoints
{
    public static void MapPairEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/pairs")
            .WithTags("Pairs")
            .RequireAuthorization();

        // GET /api/pairs — Admin/Supervisor
        group.MapGet("/", async (IPairRepo pairRepo) =>
        {
            var pairs = await pairRepo.GetAllAsync();
            return Results.Ok(pairs);
        })
        .RequireAuthorization("AdminOrSupervisor")
        .WithOpenApi();

        // GET /api/pairs/{id}
        group.MapGet("/{id:int}", async (int id, IPairRepo pairRepo) =>
        {
            var pair = await pairRepo.GetByIdAsync(id);
            return pair is null ? Results.NotFound() : Results.Ok(pair);
        })
        .WithOpenApi();

        // GET /api/pairs/by-mentor/{mentorId}
        group.MapGet("/by-mentor/{mentorId:int}", async (int mentorId, IPairRepo pairRepo) =>
        {
            var pair = await pairRepo.GetByMentorIdAsync(mentorId);
            return pair is null ? Results.NotFound() : Results.Ok(pair);
        })
        .WithOpenApi();

        // GET /api/pairs/by-mentee/{menteeId}
        group.MapGet("/by-mentee/{menteeId:int}", async (int menteeId, IPairRepo pairRepo) =>
        {
            var pair = await pairRepo.GetByMenteeIdAsync(menteeId);
            return pair is null ? Results.NotFound() : Results.Ok(pair);
        })
        .WithOpenApi();

        // GET /api/pairs/by-supervisor/{supervisorId}
        group.MapGet("/by-supervisor/{supervisorId:int}", async (int supervisorId, IPairRepo pairRepo) =>
        {
            var pairs = await pairRepo.GetBySupervisorIdAsync(supervisorId);
            return Results.Ok(pairs);
        })
        .WithOpenApi();

        // POST /api/pairs — Admin only
        group.MapPost("/", async (CreatePairRequest req, PairService pairService) =>
        {
            var result = await pairService.CreatePairAsync(req.SupervisorId, req.MentorId, req.MenteeId);
            return result.ToHttp();
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();

        // DELETE /api/pairs/{id} — Admin only
        group.MapDelete("/{id:int}", async (int id, PairService pairService) =>
        {
            var result = await pairService.SeparatePairAsync(id);
            return result.Success ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();
    }
}

record CreatePairRequest(int SupervisorId, int MentorId, int MenteeId);
