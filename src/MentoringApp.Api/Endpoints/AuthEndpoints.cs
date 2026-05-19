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
            AppSettings appSettings) =>
        {
            if (string.IsNullOrWhiteSpace(request.NationalId))
                return Results.BadRequest(new { error = "NationalId is required." });

            var result = await authService.SendVerificationCodeAsync(request.NationalId, appSettings.SkipVerificationCode);

            if (result.Success)
            {
                string? devCode = appSettings.SkipVerificationCode && !string.IsNullOrEmpty(result.Data)
                    ? result.Data
                    : null;
                return Results.Ok(new { devCode });
            }

            return Results.BadRequest(new { error = result.ErrorMessage });
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
            var result = await userService.RegisterUserAsync(req);
            return result.Success ? Results.StatusCode(201) : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .AllowAnonymous()
        .WithTags("Auth")
        .WithOpenApi();

        return app;
    }
}

