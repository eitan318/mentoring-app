using MentoringApp.Api.Helpers;
using MentoringApp.Data.Interfaces;
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
                return Results.BadRequest("NationalId is required.");

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

            return result.Success ? Results.Ok() : Results.BadRequest(result.ErrorMessage);
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
                return Results.BadRequest("NationalId and Password are required.");

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

        return app;
    }
}

public record LoginRequest(string NationalId, string Password);
public record SendCodeRequest(string NationalId);
