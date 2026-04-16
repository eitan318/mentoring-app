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
    private static readonly SemaphoreSlim Lock = new(1, 1);
    private static string? _cachedAdminToken;

    public static async Task<string> GetAdminTokenAsync()
    {
        if (_cachedAdminToken is not null)
            return _cachedAdminToken;

        await Lock.WaitAsync();
        try
        {
            // Double-checked locking
            if (_cachedAdminToken is not null)
                return _cachedAdminToken;

            _cachedAdminToken = await FetchTokenAsync();
            return _cachedAdminToken;
        }
        finally
        {
            Lock.Release();
        }
    }

    private static async Task<string> FetchTokenAsync()
    {
        // Step 1: request a verification code
        var codeResp = await Http.PostAsJsonAsync("/api/auth/send-code",
            new { nationalId = TestConfig.AdminNationalId }, JsonOpts);
        codeResp.EnsureSuccessStatusCode();

        var codeJson = await codeResp.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(codeJson))
            throw new InvalidOperationException(
                "API returned empty body for send-code — ensure ASPNETCORE_ENVIRONMENT=Development and the admin user exists.");

        var codeBody = JsonSerializer.Deserialize<SendCodeResponse>(codeJson, JsonOpts);
        var code = codeBody?.DevCode
            ?? throw new InvalidOperationException("API did not return devCode in response.");

        // Step 2: exchange code for token
        var loginResp = await Http.PostAsJsonAsync("/api/auth/login",
            new { nationalId = TestConfig.AdminNationalId, password = code }, JsonOpts);
        loginResp.EnsureSuccessStatusCode();

        var loginBody = await loginResp.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        return loginBody?.Token
            ?? throw new InvalidOperationException("Login response did not contain a token.");
    }

    private record SendCodeResponse([property: JsonPropertyName("devCode")] string? DevCode);
    private record LoginResponse([property: JsonPropertyName("token")] string? Token);
}
