using MentoringApp.ApiClient.Models;

namespace MentoringApp.ApiClient.Clients;

public class MatchingApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<IEnumerable<AvailableMentorResponse>> GetAvailableMentorsAsync() =>
        GetAsync<IEnumerable<AvailableMentorResponse>>("/api/matching/available-mentors");

    public Task<IEnumerable<AvailableMenteeResponse>> GetAvailableMenteesAsync() =>
        GetAsync<IEnumerable<AvailableMenteeResponse>>("/api/matching/available-mentees");

    public Task SendPairRequestAsync(SendPairRequestBody request) =>
        PostAsync("/api/matching/requests", request);

    public Task<IEnumerable<PairRequestResponse>> GetRequestsForMenteeAsync(int menteeId) =>
        GetAsync<IEnumerable<PairRequestResponse>>($"/api/matching/requests/for-mentee/{menteeId}");

    public Task<IEnumerable<PairRequestResponse>> GetRequestsForMentorAsync(int mentorId) =>
        GetAsync<IEnumerable<PairRequestResponse>>($"/api/matching/requests/for-mentor/{mentorId}");

    public Task AcceptRequestAsync(int requestId, AcceptRequestBody request) =>
        PutAsync($"/api/matching/requests/{requestId}/accept", request);

    public Task RejectRequestAsync(int requestId) =>
        PutAsync($"/api/matching/requests/{requestId}/reject");

    public Task CancelRequestAsync(int requestId) =>
        DeleteAsync($"/api/matching/requests/{requestId}");

    public Task<IEnumerable<MatchRecommendationResponse>> GetRecommendationsAsync(int menteeId) =>
        GetAsync<IEnumerable<MatchRecommendationResponse>>($"/api/matching/recommendations/{menteeId}");

    public Task GalleryPickAsync(GalleryPickRequest request) =>
        PostAsync("/api/matching/gallery-pick", request);

    public Task GenerateScoresAsync() =>
        PostAsync("/api/matching/pipeline/generate-scores");

    public Task<PipelineMatchResponse> RunAutoMatchAsync() =>
        PostAsync<PipelineMatchResponse>("/api/matching/pipeline/auto-match");

    public Task<PipelineMatchResponse> RunFallbackMatchAsync() =>
        PostAsync<PipelineMatchResponse>("/api/matching/pipeline/fallback-match");
}
