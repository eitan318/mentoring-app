using MentoringApp.Api.Helpers;
using MentoringApp.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MentoringApp.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
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
