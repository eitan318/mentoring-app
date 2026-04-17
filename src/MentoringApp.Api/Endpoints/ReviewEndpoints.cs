using MentoringApp.Api.Helpers;
using MentoringApp.Data.Interfaces;
using MentoringApp.Service;

namespace MentoringApp.Api.Endpoints;

public static class ReviewEndpoints
{
    public static void MapReviewEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reviews")
            .WithTags("Reviews")
            .RequireAuthorization();

        // GET /api/reviews/by-pair/{pairId}
        group.MapGet("/by-pair/{pairId:int}", async (int pairId, IReviewRepo reviewRepo) =>
        {
            var dtos = await reviewRepo.GetByPairAsync(pairId);
            return Results.Ok(dtos);
        })
        .WithOpenApi();

        // GET /api/reviews/by-author/{userId}
        group.MapGet("/by-author/{userId:int}", async (int userId, IReviewRepo reviewRepo) =>
        {
            var dtos = await reviewRepo.GetByAuthorAsync(userId);
            return Results.Ok(dtos);
        })
        .WithOpenApi();

        // POST /api/reviews — Student
        group.MapPost("/", async (CreateReviewRequest req, ReviewService reviewService) =>
        {
            var result = await reviewService.CreateReviewAsync(
                req.Content, req.PairId, req.AuthorUserId, req.AmountOfHours);
            return result.ToHttp();
        })
        .WithOpenApi();
    }
}

record CreateReviewRequest(string Content, DateTime Date, int PairId, int AuthorUserId, double AmountOfHours);
