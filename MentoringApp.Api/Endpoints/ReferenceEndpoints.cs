using MentoringApp.Api.Helpers;
using MentoringApp.Service;

namespace MentoringApp.Api.Endpoints;

public static class ReferenceEndpoints
{
    public static void MapReferenceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reference")
            .WithTags("Reference")
            .RequireAuthorization();

        // GET /api/reference/subjects
        group.MapGet("/subjects", async (SubjectService subjectService) =>
        {
            var result = await subjectService.GetAllSubjectsAsync();
            return result.ToHttp();
        })
        .WithOpenApi();

        // GET /api/reference/grades
        group.MapGet("/grades", async (GradeService gradeService) =>
        {
            var result = await gradeService.GetAllGradesAsync();
            return result.ToHttp();
        })
        .WithOpenApi();

        // GET /api/reference/school-classes (before /by-supervisor to avoid route conflict)
        group.MapGet("/school-classes/by-supervisor/{supervisorId:int}", async (int supervisorId, SchoolClassService schoolClassService) =>
        {
            var result = await schoolClassService.GetBySupervisorAsync(supervisorId);
            return result.ToHttp();
        })
        .WithOpenApi();

        // GET /api/reference/school-classes
        group.MapGet("/school-classes", async (SchoolClassService schoolClassService) =>
        {
            var result = await schoolClassService.GetAllAsync();
            return result.ToHttp();
        })
        .WithOpenApi();
    }
}
