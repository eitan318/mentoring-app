using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;

namespace MentoringApp.Web.Services;

public class AuthStateService
{
    private const string StorageKey = "auth_token";
    private string? _token;

    public string? Token => _token;
    public bool IsAuthenticated => _token != null;
    public bool IsInitialized { get; private set; }
    public int UserId { get; private set; }
    public string Role { get; private set; } = "";
    public string Language { get; private set; } = "en";

    public event Action? OnChange;

    public void Login(string token)
    {
        _token = token;
        var claims = ParseJwtPayload(token);
        if (claims.TryGetValue("sub", out var sub) && int.TryParse(sub, out var id))
            UserId = id;
        if (claims.TryGetValue("role", out var role))
            Role = role;
        if (claims.TryGetValue("language", out var lang))
            Language = lang;
        OnChange?.Invoke();
    }

    public void Logout()
    {
        _token = null;
        UserId = 0;
        Role = "";
        Language = "en";
        OnChange?.Invoke();
    }

    public async Task PersistLoginAsync(string token, IJSRuntime js)
    {
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, token);
        Login(token);
    }

    public async Task PersistLogoutAsync(IJSRuntime js)
    {
        await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        Logout();
    }

    public async Task RestoreFromStorageAsync(IJSRuntime js)
    {
        try
        {
            var token = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (token is not null)
            {
                Login(token);
                if (!Role.Equals("Student", StringComparison.OrdinalIgnoreCase))
                {
                    await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
                    Logout();
                }
            }
        }
        catch { }
        IsInitialized = true;
        OnChange?.Invoke();
    }

    private static Dictionary<string, string> ParseJwtPayload(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2) return new();
        var payload = parts[1];
        var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        try
        {
            var bytes = Convert.FromBase64String(padded);
            var json = Encoding.UTF8.GetString(bytes);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.ToString());
        }
        catch
        {
            return new();
        }
    }
}
