namespace MentoringApp.Service;

/// <summary>
/// Strongly-typed view of the "AppSettings" section of the API's appsettings.json.
/// Bound to configuration and registered as IOptions&lt;AppSettings&gt; in the API's Program.cs,
/// then injected into services that need these dev/bootstrap flags.
/// </summary>
public class AppSettings
{
    /// <summary>When true, the database schema is dropped and recreated on startup (dev only).</summary>
    public bool RecreateDbOnStartup { get; set; }
    /// <summary>When true, the email verification code step is bypassed during login (dev only).</summary>
    public bool SkipVerificationCode { get; set; }
    /// <summary>Email of the seeded initial admin, so the school can log in on first run.</summary>
    public string AdminEmail { get; set; } = "admin@school.edu";
}
