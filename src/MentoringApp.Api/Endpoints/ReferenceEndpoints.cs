using MentoringApp.Api.Helpers;
using MentoringApp.Data.Interfaces;
using MentoringApp.Service;
using MentoringApp.Model;

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

        // GET /api/reference/supervisor-for-student/{studentId}
        group.MapGet("/supervisor-for-student/{studentId:int}", async (int studentId, SchoolClassService schoolClassService) =>
        {
            var supervisorId = await schoolClassService.GetSupervisorIdForStudentAsync(studentId);
            return supervisorId.HasValue
                ? Results.Ok(new { supervisorId = supervisorId.Value })
                : Results.NotFound(new { error = "No supervisor found for this student's class." });
        })
        .WithOpenApi();

        // POST /api/reference/school-classes
        group.MapPost("/school-classes", async (AddSchoolClassRequest req, SchoolClassService schoolClassService) =>
        {
            var result = await schoolClassService.AddClassAsync(req.GradeId, req.ClassNum);
            return result.Success ? Results.StatusCode(201) : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();

        // DELETE /api/reference/school-classes/{id}
        group.MapDelete("/school-classes/{id:int}", async (int id, SchoolClassService schoolClassService) =>
        {
            var result = await schoolClassService.DeleteClassAsync(id);
            return result.Success ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();
    }
}

