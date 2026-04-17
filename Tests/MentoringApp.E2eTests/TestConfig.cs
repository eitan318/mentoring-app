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

    /// <summary>National ID of a seeded supervisor user (DummyDataSeeder: Supervisor 1).</summary>
    public static string SupervisorNationalId =>
        Environment.GetEnvironmentVariable("E2E_SUPERVISOR_NATIONAL_ID") ?? "2001";

    /// <summary>National ID of a seeded mentor student (DummyDataSeeder: first mentor).</summary>
    public static string StudentNationalId =>
        Environment.GetEnvironmentVariable("E2E_STUDENT_NATIONAL_ID") ?? "30001";

    /// <summary>localStorage key used by WasmAuthService to persist the JWT.</summary>
    public const string LocalStorageKey = "mentoring_jwt";
}
