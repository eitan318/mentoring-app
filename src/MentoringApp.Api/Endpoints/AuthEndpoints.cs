using MentoringApp.Api.Helpers;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Service;
using Microsoft.Extensions.Options;

namespace MentoringApp.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/send-code", async (
            SendCodeRequest request,
            AuthService authService,
            IWebHostEnvironment env,
            IUserRepo userRepo,
            IVerificationCodeRepo codeRepo) =>
        {
            if (string.IsNullOrWhiteSpace(request.NationalId))
                return Results.BadRequest(new { error = "NationalId is required." });

            var result = await authService.SendVerificationCodeAsync(request.NationalId);

            if (env.IsDevelopment())
            {
                // In dev, return the saved code even when email delivery fails.
                var userDto = await userRepo.GetUserDtoByNationalIdAsync(request.NationalId);
                if (userDto is not null)
                {
                    var devCode = await codeRepo.GetCodeByUserIdAsync(userDto.Id);
                    if (devCode is not null)
                        return Results.Ok(new { devCode });
                }
            }

            return result.Success ? Results.Ok() : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("SendVerificationCode")
        .WithOpenApi()
        .AllowAnonymous();

        app.MapPost("/api/auth/login", async (
            LoginRequest request,
            AuthService authService,
            IOptions<JwtSettings> jwtOptions) =>
        {
            if (string.IsNullOrWhiteSpace(request.NationalId) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { error = "NationalId and Password are required." });

            var codeResult = await authService.VerificationCodeValid(request.Password);
            if (!codeResult.Success)
                return Results.Unauthorized();

            var loginResult = await authService.LoginAsync(request.NationalId);
            if (!loginResult.Success)
                return Results.Unauthorized();

            var (token, expiresAt) = JwtHelper.GenerateToken(loginResult.Data!, jwtOptions.Value);

            return Results.Ok(new { token, expiresAt });
        })
        .WithName("Login")
        .WithOpenApi()
        .AllowAnonymous();

        // POST /api/auth/register — public self-registration
        app.MapPost("/api/auth/register", async (RegisterRequest req, UserService userService) =>
        {
            UserModel user;
            if (req.Role.Equals("supervisor", StringComparison.OrdinalIgnoreCase))
            {
                user = new SupervisorModel { UserName = req.UserName, Email = req.Email, NationalId = req.NationalId };
            }
            else
            {
                var student = new StudentModel
                {
                    UserName = req.UserName,
                    Email = req.Email,
                    NationalId = req.NationalId,
                    Grade = new GradeModel { Id = req.GradeId ?? 0, Name = string.Empty, Num = 0 },
                    ClassNum = req.ClassNum ?? 0,
                };
                if (req.PreferredMentorGender.HasValue)
                    student.PreferredMentorGender = (MentoringApp.Model.User.GenderPreference)req.PreferredMentorGender.Value;
                if (req.PreferredMenteeGender.HasValue)
                    student.PreferredMenteeGender = (MentoringApp.Model.User.GenderPreference)req.PreferredMenteeGender.Value;
                if (req.MentorSubjectId.HasValue)
                    student.MentorProfile = new MentoringApp.Model.User.StudentProfiles.MentorProfile
                        { SubjectToTeach = req.MentorSubjectId.Value, MaxMentees = req.MaxMentees ?? 1 };
                if (req.MenteeSubjectId.HasValue)
                    student.MenteeProfile = new MentoringApp.Model.User.StudentProfiles.MenteeProfile
                        { SubjectToLearn = req.MenteeSubjectId.Value };
                user = student;
            }
            user.PhoneNumber = req.PhoneNumber;
            user.Gender = (Gender)req.Gender;

            var result = await userService.CreateUserAsync(user);
            return result.Success ? Results.StatusCode(201) : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .AllowAnonymous()
        .WithTags("Auth")
        .WithOpenApi();

        return app;
    }
}

public record LoginRequest(string NationalId, string Password);
public record SendCodeRequest(string NationalId);
public record RegisterRequest(
    string UserName, string Email, string NationalId, string? PhoneNumber,
    int Gender, string Role,
    int? GradeId, int? ClassNum,
    int? PreferredMentorGender, int? PreferredMenteeGender,
    int? MentorSubjectId, int? MaxMentees,
    int? MenteeSubjectId);
