using MentoringApp.Api.Helpers;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Service;
using Microsoft.AspNetCore.Mvc;

namespace MentoringApp.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        // GET /api/users — Admin only
        group.MapGet("/", async (UserService userService) =>
        {
            var users = await userService.GetAllUsersAsync();
            return Results.Ok<IEnumerable<UserModel>>(users);
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
        group.MapGet("/{id:int}", async (int id, UserService userService) =>
        {
            var result = await userService.GetUserByIdAsync(id);
            return result.Success ? Results.Ok<UserModel>(result.Data!) : Results.NotFound();
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
                    Grade = new GradeModel { Id = 0, Name = string.Empty, Num = 0 }
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

        // PUT /api/users/{id}/mentee-profile
        group.MapPut("/{id:int}/mentee-profile", async (int id, UpdateMenteeProfileRequest req, IUserRepo userRepo) =>
        {
            await userRepo.UpsertMenteeProfileAsync(id, req.SubjectId);
            return Results.Ok();
        })
        .WithOpenApi();

        // PUT /api/users/{id}/supervisor-classes
        group.MapPut("/{id:int}/supervisor-classes", async (int id, UpdateSupervisorClassesRequest req, SchoolClassService schoolClassService) =>
        {
            var result = await schoolClassService.SetSupervisorClassesAsync(id, req.ClassIds);
            return result.Success ? Results.Ok() : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();

        // POST /api/users/{id}/profile-picture (multipart)
        group.MapPost("/{id:int}/profile-picture", async (int id, IFormFile file, IUserRepo userRepo, IWebHostEnvironment env) =>
        {
            var uploadsDir = Path.Combine(env.WebRootPath ?? env.ContentRootPath, "uploads", "profile-pictures");
            Directory.CreateDirectory(uploadsDir);
            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{id}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);
            using var stream = File.Create(filePath);
            await file.CopyToAsync(stream);
            var relativePath = Path.Combine("uploads", "profile-pictures", fileName);
            await userRepo.UpdateProfilePictureAsync(id, relativePath);
            return Results.Ok(new { path = relativePath });
        })
        .WithOpenApi()
        .DisableAntiforgery();

        // POST /api/users/import/students  (multipart Excel upload)
        group.MapPost("/import/students", async (IFormFile file, ExcelImportService importService) =>
        {
            var tempPath = Path.GetTempFileName() + ".xlsx";
            using (var stream = File.Create(tempPath))
                await file.CopyToAsync(stream);
            var result = await importService.ImportStudentsFromExcelAsync(tempPath);
            File.Delete(tempPath);
            return result.Success
                ? Results.Ok(new { imported = result.Data })
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi()
        .DisableAntiforgery();

        // POST /api/users/import/supervisors
        group.MapPost("/import/supervisors", async (IFormFile file, ExcelImportService importService) =>
        {
            var tempPath = Path.GetTempFileName() + ".xlsx";
            using (var stream = File.Create(tempPath))
                await file.CopyToAsync(stream);
            var result = await importService.ImportSupervisorsFromExcelAsync(tempPath);
            File.Delete(tempPath);
            return result.Success
                ? Results.Ok(new { imported = result.Data })
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi()
        .DisableAntiforgery();

        // GET /api/users/import/template?type=students|supervisors
        group.MapGet("/import/template", async (string type, ExcelImportService importService) =>
        {
            bool isSupervisor = type.Equals("supervisors", StringComparison.OrdinalIgnoreCase);
            var tempPath = Path.GetTempFileName() + ".xlsx";
            var result = importService.GenerateTemplate(isSupervisor, tempPath);
            if (!result.Success) return Results.BadRequest(new { error = result.ErrorMessage });
            var bytes = await File.ReadAllBytesAsync(tempPath);
            File.Delete(tempPath);
            var fileName = isSupervisor ? "supervisors_template.xlsx" : "students_template.xlsx";
            return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();
    }
}

record CreateUserRequest(string UserName, string Email, string NationalId, string? PhoneNumber, int Gender, string Role);
record UpdateBaseInfoRequest(string UserName, string Email, string NationalId, string? PhoneNumber, int Gender);
record UpdateLanguageRequest(string Language);
record UpdateGradeClassRequest(int GradeId, int ClassNum);
record UpdateGenderPreferencesRequest(int PreferredMentorGender, int PreferredMenteeGender);
record UpdateMentorProfileRequest(int SubjectId);
record UpdateMenteeProfileRequest(int SubjectId);
record UpdateSupervisorClassesRequest(IEnumerable<int> ClassIds);
