using MentoringApp.Api.Helpers;
using MentoringApp.Data.Interfaces;
using MentoringApp.Service;
using MentoringApp.Model;

namespace MentoringApp.Api.Endpoints;

public static class MatchingEndpoints
{
    public static void MapMatchingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/matching")
            .WithTags("Matching")
            .RequireAuthorization();

        // GET /api/matching/available-mentors
        group.MapGet("/available-mentors", async (MatchingFlowService matching) =>
        {
            var mentors = await matching.GetAvailableMentorsAsync();
            return Results.Ok(mentors.Select(m => new
            {
                m.Id,
                m.UserName,
                m.Email,
                m.Gender,
                SubjectToTeach = m.MentorProfile?.SubjectToTeach,
                MaxMentees = m.MentorProfile?.MaxMentees,
                m.ProfilePicturePath,
                GradeId = m.Grade?.Id,
                GradeName = m.Grade?.Name,
                ClassNum = m.ClassNum
            }));
        })
        .WithOpenApi();

        // GET /api/matching/available-mentees
        group.MapGet("/available-mentees", async (MatchingFlowService matching) =>
        {
            var mentees = await matching.GetAvailableMenteesAsync();
            return Results.Ok(mentees.Select(m => new
            {
                m.Id,
                m.UserName,
                m.Email,
                m.Gender,
                SubjectToLearn = m.MenteeProfile?.SubjectToLearn,
                m.ProfilePicturePath
            }));
        })
        .WithOpenApi();

        // POST /api/matching/requests — Student
        group.MapPost("/requests", async (SendPairRequestBody req, MatchingFlowService matching) =>
        {
            var result = await matching.SendPairRequestAsync(req.MenteeId, req.MentorId);
            return result.ToHttp();
        })
        .WithOpenApi();

        // GET /api/matching/requests/for-mentee/{menteeId}
        group.MapGet("/requests/for-mentee/{menteeId:int}", async (int menteeId, MatchingFlowService matching) =>
        {
            var requests = await matching.GetPendingRequestsForMenteeAsync(menteeId);
            return Results.Ok(requests);
        })
        .WithOpenApi();

        // GET /api/matching/requests/for-mentor/{mentorId}
        group.MapGet("/requests/for-mentor/{mentorId:int}", async (int mentorId, MatchingFlowService matching) =>
        {
            var requests = await matching.GetPendingRequestsForMentorAsync(mentorId);
            return Results.Ok(requests.Select(r => new PairRequestResponse(
                r.Id,
                r.MenteeId,
                r.MentorId,
                r.Status.ToString(),
                (int)r.Tier,
                r.CreatedAt.ToString("O"),
                r.MenteeName,
                r.MentorName,
                r.MenteeProfilePicturePath,
                (int)r.MenteeGender,
                r.MenteeSubjectName)));
        })
        .WithOpenApi();

        // PUT /api/matching/requests/{requestId}/accept — Mentor
        group.MapPut("/requests/{requestId:int}/accept", async (int requestId, MatchingFlowService matching) =>
        {
            var result = await matching.AcceptPairRequestAsync(requestId);
            return result.ToHttp();
        })
        .WithOpenApi();

        // PUT /api/matching/requests/{requestId}/reject — Mentor
        group.MapPut("/requests/{requestId:int}/reject", async (int requestId, MatchingFlowService matching) =>
        {
            var result = await matching.RejectPairRequestAsync(requestId);
            return result.ToHttp();
        })
        .WithOpenApi();

        // DELETE /api/matching/requests/{requestId} — Student (cancel)
        group.MapDelete("/requests/{requestId:int}", async (int requestId, MatchingFlowService matchingService) =>
        {
            var result = await matchingService.CancelPairRequestAsync(requestId);
            return result.Success ? Results.NoContent() : Results.NotFound();
        })
        .WithOpenApi();

        // GET /api/matching/recommendations/{menteeId}
        group.MapGet("/recommendations/{menteeId:int}", async (int menteeId, MatchingFlowService matching) =>
        {
            var recs = await matching.GetTopRecommendationsAsync(menteeId);
            return Results.Ok(recs);
        })
        .WithOpenApi();

        // POST /api/matching/gallery-pick — Student
        group.MapPost("/gallery-pick", async (GalleryPickRequest req, MatchingFlowService matching) =>
        {
            var result = await matching.GalleryPickAsync(req.MenteeId, req.MentorId);
            return result.ToHttp();
        })
        .WithOpenApi();

        // POST /api/matching/pipeline/generate-scores — Admin
        group.MapPost("/pipeline/generate-scores", async (MatchingFlowService matching) =>
        {
            var result = await matching.GenerateScoreMatrixAsync();
            return result.ToHttp();
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();

        // POST /api/matching/pipeline/auto-match — Admin
        group.MapPost("/pipeline/auto-match", async (MatchingFlowService matching) =>
        {
            var result = await matching.RunAutoMatchAsync();
            return result.Success
                ? Results.Ok(new { pairsCreated = result.Data })
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();

        // POST /api/matching/pipeline/fallback-match — Admin
        group.MapPost("/pipeline/fallback-match", async (MatchingFlowService matching) =>
        {
            var result = await matching.RunFallbackMatchAsync();
            return result.Success
                ? Results.Ok(new { pairsCreated = result.Data })
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();
    }
}
