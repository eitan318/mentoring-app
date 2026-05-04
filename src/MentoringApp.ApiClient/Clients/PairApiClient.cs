using MentoringApp.Model;
using MentoringApp.Model;

namespace MentoringApp.ApiClient.Clients;

public class PairApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<IEnumerable<PairModel>> GetAllAsync() =>
        GetAsync<IEnumerable<PairModel>>("/api/pairs");

    public Task<PairModel> GetByIdAsync(int id) =>
        GetAsync<PairModel>($"/api/pairs/{id}");

    public Task<PairModel> GetByMentorAsync(int mentorId) =>
        GetAsync<PairModel>($"/api/pairs/by-mentor/{mentorId}");

    public Task<PairModel> GetByMenteeAsync(int menteeId) =>
        GetAsync<PairModel>($"/api/pairs/by-mentee/{menteeId}");

    public Task<IEnumerable<PairModel>> GetBySupervisorAsync(int supervisorId) =>
        GetAsync<IEnumerable<PairModel>>($"/api/pairs/by-supervisor/{supervisorId}");

    public Task CreateAsync(CreatePairRequest request) =>
        PostAsync("/api/pairs", request);

    public Task DeleteAsync(int id) =>
        DeleteAsync($"/api/pairs/{id}");
}
