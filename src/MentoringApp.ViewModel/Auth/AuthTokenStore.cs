namespace MentoringApp.ViewModel.Auth;

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
