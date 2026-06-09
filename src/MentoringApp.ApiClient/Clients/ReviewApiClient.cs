using MentoringApp.Model;

namespace MentoringApp.ApiClient.Clients;

/// <summary>Typed client for the /api/reviews endpoints (session reviews by pair / by author).</summary>
public class ReviewApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<IEnumerable<ReviewResponse>> GetByPairAsync(int pairId) =>
        GetAsync<IEnumerable<ReviewResponse>>($"/api/reviews/by-pair/{pairId}");

    public Task<IEnumerable<ReviewResponse>> GetByAuthorAsync(int userId) =>
        GetAsync<IEnumerable<ReviewResponse>>($"/api/reviews/by-author/{userId}");

    public Task CreateAsync(CreateReviewRequest request) =>
        PostAsync("/api/reviews", request);
}
