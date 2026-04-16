namespace MentoringApp.E2eTests;

/// <summary>
/// Centralised configuration for E2E tests.
/// Override via environment variables in CI/CD.
/// </summary>
public static class TestConfig
{
    public static string WebBaseUrl =>
        Environment.GetEnvironmentVariable("E2E_WEB_URL") ?? "http://localhost:5041";

    public static string ApiBaseUrl =>
        Environment.GetEnvironmentVariable("E2E_API_URL") ?? "http://localhost:5035";

    /// <summary>National ID of the seeded admin user (DummyDataSeeder).</summary>
    public static string AdminNationalId =>
        Environment.GetEnvironmentVariable("E2E_ADMIN_NATIONAL_ID") ?? "100";

    /// <summary>localStorage key used by WasmAuthService to persist the JWT.</summary>
    public const string LocalStorageKey = "mentoring_jwt";
}
