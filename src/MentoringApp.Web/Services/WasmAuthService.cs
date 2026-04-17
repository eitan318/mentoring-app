using System.Text;
using System.Text.Json;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ApiClient.Models;
using MentoringApp.Components.Auth;
using Microsoft.JSInterop;

namespace MentoringApp.Web.Services;

/// <summary>
/// Scoped auth service. Delegates state storage to the singleton <see cref="AuthState"/>
/// so that <c>BearerTokenHandler</c> (resolved by IHttpClientFactory) always shares
/// the same token regardless of DI scope.
/// </summary>
public class WasmAuthService(AuthApiClient authClient, IJSRuntime js, AuthState state) : IAuthService
{
    private const string StorageKey = "mentoring_jwt";

    public string? Token => state.Token;
    public bool IsAuthenticated => state.IsAuthenticated;
    public string? Role => state.Role;
    public int? UserId => state.UserId;
    public string? Language => state.Language;

    public async Task InitAsync()
    {
        try
        {
            var stored = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrEmpty(stored))
                Apply(stored);
        }
        catch
        {
            // JS interop not yet available — ignore
        }
    }

    public async Task<string?> SendCodeAsync(string nationalId)
    {
        try
        {
            await authClient.SendCodeAsync(new SendCodeRequest(nationalId));
            return null;
        }
        catch (Exception ex) { return ex.Message; }
    }

    public async Task<bool> LoginAsync(string nationalId, string code)
    {
        try
        {
            var response = await authClient.LoginAsync(new LoginRequest(nationalId, code));
            Apply(response.Token);
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, response.Token);
            return true;
        }
        catch { return false; }
    }

    public async Task LogoutAsync()
    {
        state.Token = null;
        state.Role = null;
        state.UserId = null;
        state.Language = null;
        try { await js.InvokeVoidAsync("localStorage.removeItem", StorageKey); }
        catch { /* ignore */ }
    }

    private void Apply(string jwt)
    {
        state.Token = jwt;
        var parts = jwt.Split('.');
        if (parts.Length != 3) return;

        var payload = parts[1];
        var padded = payload.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        state.Role = root.TryGetProperty("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out var roleProp)
            ? roleProp.GetString()
            : root.TryGetProperty("role", out var rp) ? rp.GetString() : null;

        if (root.TryGetProperty("sub", out var sub) && int.TryParse(sub.GetString(), out var id))
            state.UserId = id;

        state.Language = root.TryGetProperty("language", out var lang) ? lang.GetString() : "en";
    }
}
