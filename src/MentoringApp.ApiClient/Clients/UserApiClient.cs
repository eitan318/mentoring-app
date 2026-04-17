using MentoringApp.ApiClient.Models;

namespace MentoringApp.ApiClient.Clients;

public class UserApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<IEnumerable<UserResponse>> GetAllAsync() =>
        GetAsync<IEnumerable<UserResponse>>("/api/users");

    public Task<UserResponse> GetByIdAsync(int id) =>
        GetAsync<UserResponse>($"/api/users/{id}");

    public Task<IEnumerable<SupervisorStatsResponse>> GetSupervisorStatsAsync() =>
        GetAsync<IEnumerable<SupervisorStatsResponse>>("/api/users/supervisors/stats");

    public Task CreateAsync(CreateUserRequest request) =>
        PostAsync("/api/users", request);

    public Task DeleteAsync(int id) =>
        DeleteAsync($"/api/users/{id}");

    public Task UpdateBaseInfoAsync(int id, UpdateBaseInfoRequest request) =>
        PutAsync($"/api/users/{id}/base-info", request);

    public Task UpdateLanguageAsync(int id, UpdateLanguageRequest request) =>
        PutAsync($"/api/users/{id}/language", request);

    public Task UpdateGradeClassAsync(int id, UpdateGradeClassRequest request) =>
        PutAsync($"/api/users/{id}/grade-class", request);

    public Task UpdateGenderPreferencesAsync(int id, UpdateGenderPreferencesRequest request) =>
        PutAsync($"/api/users/{id}/gender-preferences", request);

    public Task UpdateMentorProfileAsync(int id, UpdateMentorProfileRequest request) =>
        PutAsync($"/api/users/{id}/mentor-profile", request);
}
