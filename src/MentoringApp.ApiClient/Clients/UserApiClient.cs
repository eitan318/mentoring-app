using System.Net.Http.Json;
using MentoringApp.ApiClient.Models;
using MentoringApp.Model.User;

namespace MentoringApp.ApiClient.Clients;

public class UserApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<IEnumerable<UserModel>> GetAllAsync() =>
        GetAsync<IEnumerable<UserModel>>("/api/users");

    public Task<UserModel> GetByIdAsync(int id) =>
        GetAsync<UserModel>($"/api/users/{id}");

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

    public Task UpdateMenteeProfileAsync(int id, UpdateMenteeProfileRequest request) =>
        PutAsync($"/api/users/{id}/mentee-profile", request);

    public Task UpdateSupervisorClassesAsync(int id, UpdateSupervisorClassesRequest request) =>
        PutAsync($"/api/users/{id}/supervisor-classes", request);

    public async Task<string?> UploadProfilePictureAsync(int id, string filePath)
    {
        using var form = new MultipartFormDataContent();
        using var fileStream = File.OpenRead(filePath);
        form.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));
        var response = await Http.PostAsync($"/api/users/{id}/profile-picture", form);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<UploadResult>();
        return result?.Path;
    }

    public async Task<int> ImportStudentsAsync(string filePath)
    {
        using var form = new MultipartFormDataContent();
        using var fileStream = File.OpenRead(filePath);
        form.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));
        var response = await Http.PostAsync("/api/users/import/students", form);
        if (!response.IsSuccessStatusCode) throw new Exception($"Import failed: {response.StatusCode}");
        var result = await response.Content.ReadFromJsonAsync<ImportResult>();
        return result?.Imported ?? 0;
    }

    public async Task<int> ImportSupervisorsAsync(string filePath)
    {
        using var form = new MultipartFormDataContent();
        using var fileStream = File.OpenRead(filePath);
        form.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));
        var response = await Http.PostAsync("/api/users/import/supervisors", form);
        if (!response.IsSuccessStatusCode) throw new Exception($"Import failed: {response.StatusCode}");
        var result = await response.Content.ReadFromJsonAsync<ImportResult>();
        return result?.Imported ?? 0;
    }

    public async Task DownloadTemplateAsync(bool isSupervisor, string savePath)
    {
        var type = isSupervisor ? "supervisors" : "students";
        var response = await Http.GetAsync($"/api/users/import/template?type={type}");
        if (!response.IsSuccessStatusCode) throw new Exception($"Download failed: {response.StatusCode}");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(savePath, bytes);
    }

    private record UploadResult(string Path);
    private record ImportResult(int Imported);
}
