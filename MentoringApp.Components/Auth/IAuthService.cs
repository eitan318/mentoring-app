namespace MentoringApp.Components.Auth;

public interface IAuthService
{
    Task<string?> SendCodeAsync(string nationalId);   // returns error message or null on success
    Task<bool> LoginAsync(string nationalId, string code);
    Task LogoutAsync();
    bool IsAuthenticated { get; }
    string? Token { get; }
    string? Role { get; }
    int? UserId { get; }
    string? Language { get; }
}
