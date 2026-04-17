using MentoringApp.E2eTests.Helpers;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace MentoringApp.E2eTests.Tests.Student;

/// <summary>
/// E2E tests for the student (mentor/mentee) client.
/// Uses a seeded mentor student (nationalId = "30001") from DummyDataSeeder.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class StudentDashboardTests : PageTest
{
    private string _token = string.Empty;

    [SetUp]
    public async Task SetUp()
    {
        _token = await AuthHelper.GetStudentTokenAsync();
    }

    [Test]
    public async Task StudentDashboard_Loads_WithoutRedirectToLogin()
    {
        await Page.NavigateAuthenticatedAsync("/student", _token);
        await Page.WaitForBlazorAsync();

        // Student must stay on the dashboard, not be bounced to login.
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/student"));
    }

    [Test]
    public async Task StudentDashboard_ShowsDashboardHeading()
    {
        await Page.NavigateAuthenticatedAsync("/student", _token);
        await Page.WaitForBlazorAsync();

        await Expect(Page.Locator("h2")).ToBeVisibleAsync();
    }

    [Test]
    public async Task StudentDashboard_ShowsSidebarNavigation()
    {
        await Page.NavigateAuthenticatedAsync("/student", _token);
        await Page.WaitForBlazorAsync();

        // Sidebar navigation should be present (rendered by MainLayout after auth check)
        await Expect(Page.Locator("nav.sidebar")).ToBeVisibleAsync();
    }

    [Test]
    public async Task UnauthenticatedAccess_ToStudentPage_RedirectsToLogin()
    {
        // Navigate without a token — should redirect to /login
        await Page.GotoAsync(TestConfig.WebBaseUrl + "/student");
        await Page.WaitForBlazorAsync();

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/login$"));
    }

    [Test]
    public async Task StudentDashboard_DoesNotShowAdminControls()
    {
        await Page.NavigateAuthenticatedAsync("/student", _token);
        await Page.WaitForBlazorAsync();

        // Admin-only controls must not be visible to a student
        var adminLink = Page.GetByRole(AriaRole.Link, new() { Name = "Users" });
        await Expect(adminLink).Not.ToBeVisibleAsync();
    }
}
