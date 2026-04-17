using MentoringApp.Api.Helpers;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Service;

namespace MentoringApp.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        // GET /api/users — Admin only
        group.MapGet("/", async (IUserRepo userRepo) =>
        {
            var dtos = await userRepo.GetAllUserDtosAsync();
            return Results.Ok(dtos);
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();

        // GET /api/users/supervisors/stats — Admin only (must be before /{id})
        group.MapGet("/supervisors/stats", async (IUserRepo userRepo) =>
        {
            var stats = await userRepo.GetSupervisorStatisticsAsync();
            return Results.Ok(stats);
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();

        // GET /api/users/{id}
        group.MapGet("/{id:int}", async (int id, IUserRepo userRepo) =>
        {
            var dto = await userRepo.GetUserDtoByIdAsync(id);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithOpenApi();

        // POST /api/users — Admin only
        group.MapPost("/", async (CreateUserRequest req, UserService userService) =>
        {
            UserModel user = req.Role.ToLowerInvariant() switch
            {
                "admin" => new AdminModel(0, req.Email, req.UserName, req.NationalId),
                "supervisor" => new SupervisorModel(0, req.Email, req.UserName, req.NationalId),
                _ => new StudentModel
                {
                    Email = req.Email,
                    UserName = req.UserName,
                    NationalId = req.NationalId,
                    Grade = new Grade { Id = 0, Name = string.Empty, Num = 0 }
                }
            };
            user.PhoneNumber = req.PhoneNumber;
            user.Gender = (Gender)req.Gender;

            var result = await userService.CreateUserAsync(user);
            return result.Success ? Results.StatusCode(201) : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();

        // DELETE /api/users/{id} — Admin only
        group.MapDelete("/{id:int}", async (int id, UserService userService) =>
        {
            var result = await userService.DeleteUserAsync(id);
            return result.Success ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();

        // PUT /api/users/{id}/base-info
        group.MapPut("/{id:int}/base-info", async (int id, UpdateBaseInfoRequest req, IUserRepo userRepo) =>
        {
            bool ok = await userRepo.UpdateBaseInfoAsync(id, req.UserName, req.Email, req.NationalId, req.PhoneNumber, req.Gender);
            return ok ? Results.Ok() : Results.NotFound();
        })
        .WithOpenApi();

        // PUT /api/users/{id}/language
        group.MapPut("/{id:int}/language", async (int id, UpdateLanguageRequest req, UserService userService) =>
        {
            var result = await userService.UpdateLanguageAsync(id, req.Language);
            return result.ToHttp();
        })
        .WithOpenApi();

        // PUT /api/users/{id}/grade-class
        group.MapPut("/{id:int}/grade-class", async (int id, UpdateGradeClassRequest req, IUserRepo userRepo) =>
        {
            await userRepo.UpdateStudentGradeAndClassAsync(id, req.GradeId, req.ClassNum);
            return Results.Ok();
        })
        .WithOpenApi();

        // PUT /api/users/{id}/gender-preferences
        group.MapPut("/{id:int}/gender-preferences", async (int id, UpdateGenderPreferencesRequest req, IUserRepo userRepo) =>
        {
            await userRepo.UpdateStudentPreferredGendersAsync(id, req.PreferredMentorGender, req.PreferredMenteeGender);
            return Results.Ok();
        })
        .WithOpenApi();

        // PUT /api/users/{id}/mentor-profile
        group.MapPut("/{id:int}/mentor-profile", async (int id, UpdateMentorProfileRequest req, IUserRepo userRepo) =>
        {
            await userRepo.UpsertMentorProfileAsync(id, req.SubjectId);
            return Results.Ok();
        })
        .WithOpenApi();
    }
}

record CreateUserRequest(string UserName, string Email, string NationalId, string? PhoneNumber, int Gender, string Role);
record UpdateBaseInfoRequest(string UserName, string Email, string NationalId, string? PhoneNumber, int Gender);
record UpdateLanguageRequest(string Language);
record UpdateGradeClassRequest(int GradeId, int ClassNum);
record UpdateGenderPreferencesRequest(int PreferredMentorGender, int PreferredMenteeGender);
record UpdateMentorProfileRequest(int SubjectId);
