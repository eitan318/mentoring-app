namespace MentoringApp.ViewModel.Auth;

public class AuthTokenStore
{
    public string? Token { get; set; }
    public int? UserId { get; set; }
    public string? Role { get; set; }
    public string? Language { get; set; }

    public bool IsAuthenticated => Token != null;

    public void Clear()
    {
        Token = null;
        UserId = null;
        Role = null;
        Language = null;
    }
}
