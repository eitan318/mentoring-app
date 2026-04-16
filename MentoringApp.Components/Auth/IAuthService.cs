namespace MentoringApp.Components.Auth;

public interface IAuthService
{
    /// <summary>Restores auth state from persistent storage (e.g. localStorage). Call once on app start.</summary>
    Task InitAsync();
    Task<string?> SendCodeAsync(string nationalId);   // returns error message or null on success
    Task<bool> LoginAsync(string nationalId, string code);
    Task LogoutAsync();

    // Delegates to the shared AuthState singleton so BearerTokenHandler always reads the right value.
    bool IsAuthenticated { get; }
    string? Token { get; }
    string? Role { get; }
    int? UserId { get; }
    string? Language { get; }
}
