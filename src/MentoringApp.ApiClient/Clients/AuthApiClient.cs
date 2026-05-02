using MentoringApp.Model;

namespace MentoringApp.ApiClient.Clients;

public class AuthApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<SendCodeResponse> SendCodeAsync(SendCodeRequest request) =>
        PostAsync<SendCodeResponse>("/api/auth/send-code", request);

    public Task<LoginResponse> LoginAsync(LoginRequest request) =>
        PostAsync<LoginResponse>("/api/auth/login", request);

    public Task RegisterAsync(RegisterRequest request) =>
        PostAsync("/api/auth/register", request);
}
