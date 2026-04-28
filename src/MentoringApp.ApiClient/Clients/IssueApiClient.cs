using MentoringApp.ApiClient.Models;
using MentoringApp.Model;

namespace MentoringApp.ApiClient.Clients;

public class IssueApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<IEnumerable<IssueModel>> GetAllAsync() =>
        GetAsync<IEnumerable<IssueModel>>("/api/issues");

    public Task<IssueModel> GetByIdAsync(int id) =>
        GetAsync<IssueModel>($"/api/issues/{id}");

    public Task<IEnumerable<IssueModel>> GetByUserAsync(int userId) =>
        GetAsync<IEnumerable<IssueModel>>($"/api/issues/by-user/{userId}");

    public Task<IEnumerable<IssueModel>> GetBySupervisorAsync(int supervisorId) =>
        GetAsync<IEnumerable<IssueModel>>($"/api/issues/by-supervisor/{supervisorId}");

    public Task<IEnumerable<IssueModel>> GetForwardedAsync() =>
        GetAsync<IEnumerable<IssueModel>>("/api/issues/forwarded");

    public Task<IEnumerable<IssueCategoryModel>> GetCategoriesAsync() =>
        GetAsync<IEnumerable<IssueCategoryModel>>("/api/issues/categories");

    public Task CreateAsync(CreateIssueRequest request) =>
        PostAsync("/api/issues", request);

    public Task ResolveAsync(int id) =>
        PutAsync($"/api/issues/{id}/resolve");

    public Task ForwardAsync(int id, ForwardIssueRequest request) =>
        PutAsync($"/api/issues/{id}/forward", request);
}
