namespace MentoringApp.Components.Auth;

/// <summary>
/// Singleton that carries the authenticated user's JWT and parsed claims.
/// Shared between <see cref="IAuthService"/> (sets it) and
/// <c>BearerTokenHandler</c> (reads <see cref="Token"/> for each request).
/// Using a dedicated singleton avoids the IHttpClientFactory scope-capture problem
/// that occurs when DelegatingHandlers are resolved before components initialize.
/// </summary>
public sealed class AuthState
{
    public string? Token { get; set; }
    public string? Role { get; set; }
    public int? UserId { get; set; }
    public string? Language { get; set; }

    public bool IsAuthenticated => Token is not null;
}
