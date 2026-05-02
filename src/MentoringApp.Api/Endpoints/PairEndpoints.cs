using DocumentFormat.OpenXml.Office2010.Excel;
using MentoringApp.Api.Helpers;
using MentoringApp.Data.Interfaces;
using MentoringApp.Service;
using MentoringApp.Model;

namespace MentoringApp.Api.Endpoints;

public static class PairEndpoints
{
    public static void MapPairEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/pairs")
            .WithTags("Pairs")
            .RequireAuthorization();

        // GET /api/pairs — Admin/Supervisor
        group.MapGet("/", async (PairService pairService) =>
            (await pairService.GetAllPairsAsync())
            .ToHttp())

            .RequireAuthorization("AdminOrSupervisor")
            .WithOpenApi();

        // GET /api/pairs/{id}
        group.MapGet("/{id:int}", async (int id, PairService pairService) =>
            (await pairService.GetPairByIdAsync(id))
            .ToHttp())
            .WithOpenApi();

        // GET /api/pairs/by-mentor/{mentorId}
        group.MapGet("/by-mentor/{mentorId:int}", async (int mentorId, PairService pairService) =>
            (await pairService.GetPairByMentorIdAsync(mentorId))
            .ToHttp())
            .WithOpenApi();

        // GET /api/pairs/by-mentee/{menteeId}
        group.MapGet("/by-mentee/{menteeId:int}", async (int menteeId, PairService pairService) =>
            (await pairService.GetPairByMenteeIdAsync(menteeId))
            .ToHttp())
            .WithOpenApi();

        // GET /api/pairs/by-supervisor/{supervisorId}
        group.MapGet("/by-supervisor/{id}", async(int id, PairService svc) =>
            (await svc.GetPairsBySupervisorAsync(id))
            .ToHttp());

        // POST /api/pairs — Admin only
        group.MapPost("/", async (CreatePairRequest req, PairService pairService) =>
            (await pairService.CreatePairAsync(req.SupervisorId, req.MentorId, req.MenteeId))
            .ToHttp())
            .RequireAuthorization("AdminOnly")
            .WithOpenApi();

        // DELETE /api/pairs/{id} — Admin only
        group.MapDelete("/{id:int}", async (int id, PairService pairService) =>
            (await pairService.SeparatePairAsync(id))
            .ToHttp())
            .RequireAuthorization("AdminOnly")
            .WithOpenApi();
    }
}

