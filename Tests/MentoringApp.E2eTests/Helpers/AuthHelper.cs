using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MentoringApp.E2eTests.Helpers;

/// <summary>
/// Gets a JWT token directly from the API, bypassing the UI login flow.
/// The token is fetched once per test session and cached — parallel tests
/// all share the same token to avoid send-code race conditions.
/// </summary>
public static class AuthHelper
{
    private static readonly HttpClient Http = new() { BaseAddress = new Uri(TestConfig.ApiBaseUrl) };
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // Per-role token caches so parallel test classes don't race each other.
    private static readonly SemaphoreSlim GlobalLock = new(1, 1);
    private static readonly Dictionary<string, string> TokenCache = new();

    public static Task<string> GetAdminTokenAsync()      => GetOrFetchAsync(TestConfig.AdminNationalId);
    public static Task<string> GetSupervisorTokenAsync() => GetOrFetchAsync(TestConfig.SupervisorNationalId);
    public static Task<string> GetStudentTokenAsync()    => GetOrFetchAsync(TestConfig.StudentNationalId);

    private static async Task<string> GetOrFetchAsync(string nationalId)
    {
        if (TokenCache.TryGetValue(nationalId, out var cached)) return cached;

        await GlobalLock.WaitAsync();
        try
        {
            if (TokenCache.TryGetValue(nationalId, out cached)) return cached;
            var token = await FetchTokenAsync(nationalId);
            TokenCache[nationalId] = token;
            return token;
        }
        finally
        {
            GlobalLock.Release();
        }
    }

    private static async Task<string> FetchTokenAsync(string nationalId)
    {
        // Step 1: request a verification code
        var codeResp = await Http.PostAsJsonAsync("/api/auth/send-code",
            new { nationalId }, JsonOpts);
        codeResp.EnsureSuccessStatusCode();

        var codeJson = await codeResp.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(codeJson))
            throw new InvalidOperationException(
                $"API returned empty body for send-code (nationalId={nationalId}). " +
                "Ensure ASPNETCORE_ENVIRONMENT=Development and the user exists in the seed.");

        var codeBody = JsonSerializer.Deserialize<SendCodeResponse>(codeJson, JsonOpts);
        var code = codeBody?.DevCode
            ?? throw new InvalidOperationException("API did not return devCode in response.");

        // Step 2: exchange code for token
        var loginResp = await Http.PostAsJsonAsync("/api/auth/login",
            new { nationalId, password = code }, JsonOpts);
        loginResp.EnsureSuccessStatusCode();

        var loginBody = await loginResp.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        return loginBody?.Token
            ?? throw new InvalidOperationException("Login response did not contain a token.");
    }

    private record SendCodeResponse([property: JsonPropertyName("devCode")] string? DevCode);
    private record LoginResponse([property: JsonPropertyName("token")] string? Token);
}
