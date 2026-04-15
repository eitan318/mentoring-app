using MentoringApp.ApiClient.Models;

namespace MentoringApp.ApiClient.Clients;

public class PairApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<IEnumerable<PairResponse>> GetAllAsync() =>
        GetAsync<IEnumerable<PairResponse>>("/api/pairs");

    public Task<PairResponse> GetByIdAsync(int id) =>
        GetAsync<PairResponse>($"/api/pairs/{id}");

    public Task<PairResponse> GetByMentorAsync(int mentorId) =>
        GetAsync<PairResponse>($"/api/pairs/by-mentor/{mentorId}");

    public Task<PairResponse> GetByMenteeAsync(int menteeId) =>
        GetAsync<PairResponse>($"/api/pairs/by-mentee/{menteeId}");

    public Task<IEnumerable<PairResponse>> GetBySupervisorAsync(int supervisorId) =>
        GetAsync<IEnumerable<PairResponse>>($"/api/pairs/by-supervisor/{supervisorId}");

    public Task CreateAsync(CreatePairRequest request) =>
        PostAsync("/api/pairs", request);

    public Task DeleteAsync(int id) =>
        DeleteAsync($"/api/pairs/{id}");
}
