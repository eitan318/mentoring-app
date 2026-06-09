namespace MentoringApp.Api.Helpers;

/// <summary>
/// Strongly-typed view of the "JwtSettings" section of appsettings.json. Bound in Program.cs
/// (as IOptions&lt;JwtSettings&gt;) and used both to sign tokens (<see cref="JwtHelper"/>) and to
/// configure token validation parameters.
/// </summary>
public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryHours { get; set; } = 8;
}
