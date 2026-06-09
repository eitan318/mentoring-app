namespace MentoringApp.ViewModel.Auth;

/// <summary>
/// In-memory holder for the current JWT and the logged-in user's id/role/language (singleton).
/// Read by <see cref="BearerTokenHandler"/> to attach the token to outgoing requests, and raises
/// <see cref="SessionExpired"/> (once) when the API returns 401 so the app can return to login.
/// </summary>
public class AuthTokenStore
{
    public event Action? SessionExpired;

    public string? Token { get; set; }
    public int? UserId { get; set; }
    public string? Role { get; set; }
    public string? Language { get; set; }

    public bool IsAuthenticated => Token != null;

    private bool _sessionExpiredNotified;

    public void Clear()
    {
        Token = null;
        UserId = null;
        Role = null;
        Language = null;
        _sessionExpiredNotified = false;
    }

    internal void NotifySessionExpired()
    {
        if (_sessionExpiredNotified) return;
        _sessionExpiredNotified = true;
        Clear();
        SessionExpired?.Invoke();
    }
}
