using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ApiClient.Models;
using MentoringApp.Components.Auth;

namespace MentoringApp.Web.Services;

public class WasmAuthService(AuthApiClient authClient) : IAuthService
{
    private string? _token;
    private string? _role;
    private int? _userId;
    private string? _language;

    public string? Token => _token;
    public bool IsAuthenticated => _token is not null;
    public string? Role => _role;
    public int? UserId => _userId;
    public string? Language => _language;

    public async Task<string?> SendCodeAsync(string nationalId)
    {
        try
        {
            await authClient.SendCodeAsync(new SendCodeRequest(nationalId));
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public async Task<bool> LoginAsync(string nationalId, string code)
    {
        try
        {
            var response = await authClient.LoginAsync(new LoginRequest(nationalId, code));
            _token = response.Token;
            ParseClaims(response.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task LogoutAsync()
    {
        _token = null;
        _role = null;
        _userId = null;
        _language = null;
        return Task.CompletedTask;
    }

    private void ParseClaims(string jwt)
    {
        // JWT is three base64url sections separated by '.'
        var parts = jwt.Split('.');
        if (parts.Length != 3) return;

        var payload = parts[1];
        // base64url → base64 padding
        var padded = payload.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        _role = root.TryGetProperty("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out var roleProp)
            ? roleProp.GetString()
            : root.TryGetProperty("role", out var rp) ? rp.GetString() : null;

        if (root.TryGetProperty("sub", out var sub) && int.TryParse(sub.GetString(), out var id))
            _userId = id;

        _language = root.TryGetProperty("language", out var lang) ? lang.GetString() : "en";
    }
}
