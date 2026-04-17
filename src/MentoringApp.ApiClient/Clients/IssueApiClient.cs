using MentoringApp.ApiClient.Models;

namespace MentoringApp.ApiClient.Clients;

public class IssueApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<IEnumerable<IssueResponse>> GetAllAsync() =>
        GetAsync<IEnumerable<IssueResponse>>("/api/issues");

    public Task<IEnumerable<IssueResponse>> GetByUserAsync(int userId) =>
        GetAsync<IEnumerable<IssueResponse>>($"/api/issues/by-user/{userId}");

    public Task<IEnumerable<IssueResponse>> GetBySupervisorAsync(int supervisorId) =>
        GetAsync<IEnumerable<IssueResponse>>($"/api/issues/by-supervisor/{supervisorId}");

    public Task<IEnumerable<IssueResponse>> GetForwardedAsync() =>
        GetAsync<IEnumerable<IssueResponse>>("/api/issues/forwarded");

    public Task<IEnumerable<IssueCategoryResponse>> GetCategoriesAsync() =>
        GetAsync<IEnumerable<IssueCategoryResponse>>("/api/issues/categories");

    public Task CreateAsync(CreateIssueRequest request) =>
        PostAsync("/api/issues", request);

    public Task ResolveAsync(int id) =>
        PutAsync($"/api/issues/{id}/resolve");

    public Task ForwardAsync(int id, ForwardIssueRequest request) =>
        PutAsync($"/api/issues/{id}/forward", request);
}
