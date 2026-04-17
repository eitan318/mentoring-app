using MentoringApp.ApiClient.Models;

namespace MentoringApp.ApiClient.Clients;

public class AuthApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task SendCodeAsync(SendCodeRequest request) =>
        PostAsync("/api/auth/send-code", request);

    public Task<LoginResponse> LoginAsync(LoginRequest request) =>
        PostAsync<LoginResponse>("/api/auth/login", request);
}
