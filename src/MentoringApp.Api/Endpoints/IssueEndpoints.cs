using MentoringApp.Api.Helpers;
using MentoringApp.Service;
using MentoringApp.Model;

namespace MentoringApp.Api.Endpoints;

public static class IssueEndpoints
{
    public static void MapIssueEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/issues")
            .WithTags("Issues")
            .RequireAuthorization();

        // GET /api/issues — Admin
        group.MapGet("/", async (IssueService issueService) =>
            (await issueService.GetAllIssuesAsync()).ToHttp())
            .RequireAuthorization("AdminOnly")
            .WithOpenApi();

        // GET /api/issues/forwarded — Admin (before /{id} to avoid conflict)
        group.MapGet("/forwarded", async (IssueService issueService) =>
            (await issueService.GetForwardedIssuesAsync()).ToHttp())
            .RequireAuthorization("AdminOnly")
            .WithOpenApi();

        // GET /api/issues/categories
        group.MapGet("/categories", async (IssueService issueService) =>
            (await issueService.GetCategoriesAsync()).ToHttp())
            .WithOpenApi();

        // GET /api/issues/{id}
        group.MapGet("/{id:int}", async (int id, IssueService issueService) =>
            (await issueService.GetIssueByIdAsync(id)).ToHttp())
            .WithOpenApi();

        // GET /api/issues/by-user/{userId}
        group.MapGet("/by-user/{userId:int}", async (int userId, IssueService issueService) =>
            (await issueService.GetIssuesByUserAsync(userId)).ToHttp())
            .WithOpenApi();

        // GET /api/issues/by-supervisor/{supervisorId}
        group.MapGet("/by-supervisor/{supervisorId:int}", async (int supervisorId, IssueService issueService) =>
        (await issueService.GetIssuesBySupervisorAsync(supervisorId)).ToHttp())
            .WithOpenApi();

        // POST /api/issues
        group.MapPost("/", async (CreateIssueRequest req, IssueService issueService) =>
            (await issueService.CreateIssueAsync(req.Description, req.CategoryId, req.ReportedByUserId)).ToHttp())
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
