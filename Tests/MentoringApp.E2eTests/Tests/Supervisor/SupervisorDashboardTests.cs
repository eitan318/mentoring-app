using MentoringApp.E2eTests.Helpers;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace MentoringApp.E2eTests.Tests.Supervisor;

/// <summary>
/// E2E tests for the supervisor client.
/// Uses a seeded supervisor (nationalId = "2001") from DummyDataSeeder.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class SupervisorDashboardTests : PageTest
{
    private string _token = string.Empty;

    [SetUp]
    public async Task SetUp()
    {
        _token = await AuthHelper.GetSupervisorTokenAsync();
    }

    [Test]
    public async Task SupervisorDashboard_Loads_WithoutRedirectToLogin()
    {
        await Page.NavigateAuthenticatedAsync("/supervisor", _token);
        await Page.WaitForBlazorAsync();

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/supervisor"));
    }

    [Test]
    public async Task SupervisorDashboard_ShowsDashboardHeading()
    {
        await Page.NavigateAuthenticatedAsync("/supervisor", _token);
        await Page.WaitForBlazorAsync();

        await Expect(Page.Locator("h2")).ToBeVisibleAsync();
    }

    [Test]
    public async Task SupervisorDashboard_ShowsSidebarNavigation()
    {
        await Page.NavigateAuthenticatedAsync("/supervisor", _token);
        await Page.WaitForBlazorAsync();

        await Expect(Page.Locator("nav.sidebar")).ToBeVisibleAsync();
    }

    [Test]
    public async Task UnauthenticatedAccess_ToSupervisorPage_RedirectsToLogin()
    {
        await Page.GotoAsync(TestConfig.WebBaseUrl + "/supervisor");
        await Page.WaitForBlazorAsync();

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/login$"));
    }

    [Test]
    public async Task SupervisorDashboard_DoesNotShowAdminControls()
    {
        await Page.NavigateAuthenticatedAsync("/supervisor", _token);
        await Page.WaitForBlazorAsync();

        // Admin-only "Users" management link must not appear for a supervisor
        var adminLink = Page.GetByRole(AriaRole.Link, new() { Name = "Users" });
        await Expect(adminLink).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task SupervisorDashboard_ShowsIssuesSection()
    {
        await Page.NavigateAuthenticatedAsync("/supervisor", _token);
        await Page.WaitForBlazorAsync();

        // Supervisors should see an issues management section
        await Expect(Page.GetByText("Issues", new() { Exact = false })).ToBeVisibleAsync();
    }
}
