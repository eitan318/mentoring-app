using MentoringApp.Api.Helpers;
using MentoringApp.Data.Interfaces;
using MentoringApp.Service;

namespace MentoringApp.Api.Endpoints;

public static class IssueEndpoints
{
    public static void MapIssueEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/issues")
            .WithTags("Issues")
            .RequireAuthorization();

        // GET /api/issues — Admin
        group.MapGet("/", async (IIssueRepo issueRepo) =>
        {
            var dtos = await issueRepo.GetAllAsync();
            return Results.Ok(dtos);
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();

        // GET /api/issues/forwarded — Admin (before /{id} to avoid conflict)
        group.MapGet("/forwarded", async (IIssueRepo issueRepo) =>
        {
            var dtos = await issueRepo.GetForwardedAsync();
            return Results.Ok(dtos);
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();

        // GET /api/issues/categories
        group.MapGet("/categories", async (IIssueCategoryRepo categoryRepo) =>
        {
            var dtos = await categoryRepo.GetAllAsync();
            return Results.Ok(dtos);
        })
        .WithOpenApi();

        // GET /api/issues/{id}
        group.MapGet("/{id:int}", async (int id, IIssueRepo issueRepo) =>
        {
            var dto = await issueRepo.GetByIdAsync(id);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithOpenApi();

        // GET /api/issues/by-user/{userId}
        group.MapGet("/by-user/{userId:int}", async (int userId, IIssueRepo issueRepo) =>
        {
            var dtos = await issueRepo.GetByReporterAsync(userId);
            return Results.Ok(dtos);
        })
        .WithOpenApi();

        // GET /api/issues/by-supervisor/{supervisorId}
        group.MapGet("/by-supervisor/{supervisorId:int}", async (int supervisorId, IIssueRepo issueRepo) =>
        {
            var dtos = await issueRepo.GetBySupervisorAsync(supervisorId);
            return Results.Ok(dtos);
        })
        .WithOpenApi();

        // POST /api/issues
        group.MapPost("/", async (CreateIssueRequest req, IssueService issueService) =>
        {
            var result = await issueService.CreateIssueAsync(req.Description, req.CategoryId, req.ReportedByUserId);
            return result.ToHttp();
        })
        .WithOpenApi();

        // PUT /api/issues/{id}/resolve — Admin/Supervisor
        group.MapPut("/{id:int}/resolve", async (int id, IssueService issueService) =>
        {
            var result = await issueService.ResolveIssueAsync(id);
            return result.ToHttp();
        })
        .RequireAuthorization("AdminOrSupervisor")
        .WithOpenApi();

        // PUT /api/issues/{id}/forward — Supervisor
        group.MapPut("/{id:int}/forward", async (int id, ForwardIssueRequest req, IssueService issueService) =>
        {
            var result = await issueService.ForwardIssueAsync(id, req.SupervisorId);
            return result.ToHttp();
        })
        .RequireAuthorization("AdminOrSupervisor")
        .WithOpenApi();
    }
}

record CreateIssueRequest(string Description, int CategoryId, int ReportedByUserId);
record ForwardIssueRequest(int SupervisorId);
