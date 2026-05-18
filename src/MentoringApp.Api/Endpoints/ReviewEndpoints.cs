using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MentoringApp.Api.Helpers;
using MentoringApp.Data.Interfaces;
using MentoringApp.Service;
using MentoringApp.Model;

namespace MentoringApp.Api.Endpoints;

public static class ReviewEndpoints
{
    public static void MapReviewEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reviews")
            .WithTags("Reviews")
            .RequireAuthorization();

        // GET /api/reviews/by-pair/{pairId}
        group.MapGet("/by-pair/{pairId:int}", async (int pairId, ReviewService reviewService) =>
        {
            var result = await reviewService.GetReviewsByPairAsync(pairId);
            return result.ToHttp();
        })
        .WithOpenApi();

        // GET /api/reviews/by-author/{userId}
        group.MapGet("/by-author/{userId:int}", async (int userId, ReviewService reviewService) =>
        {
            var result = await reviewService.GetReviewsByAuthorAsync(userId);
            return result.ToHttp();
        })
        .WithOpenApi();

        // POST /api/reviews — Student
        group.MapPost("/", async (CreateReviewRequest req, ClaimsPrincipal user, ReviewService reviewService) =>
        {
            if (req.Content.Length < 10 || req.Content.Length > 2000)
                return Results.BadRequest(new { error = "Review content must be between 10 and 2000 characters." });
            if (req.AmountOfHours < 0.1 || req.AmountOfHours > 24)
                return Results.BadRequest(new { error = "Hours must be between 0.1 and 24." });

            if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out int userId))
                return Results.Unauthorized();

            var result = await reviewService.CreateReviewAsync(req.Content, req.PairId, userId, req.AmountOfHours);
            return result.ToHttp();
        })
        .WithOpenApi();
    }
}
