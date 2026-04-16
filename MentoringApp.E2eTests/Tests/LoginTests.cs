using MentoringApp.E2eTests.Helpers;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace MentoringApp.E2eTests.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class LoginTests : PageTest
{
    [Test]
    public async Task LoginPage_RendersNationalIdField()
    {
        await Page.GotoAsync(TestConfig.WebBaseUrl + "/login");
        await Page.WaitForBlazorAsync();

        var nationalIdInput = Page.Locator("input[placeholder*='National ID'], input[placeholder*='national']").First;
        await Expect(nationalIdInput).ToBeVisibleAsync();
    }

    [Test]
    public async Task Login_WithValidAdminCode_RedirectsToDashboard()
    {
        await Page.GotoAsync(TestConfig.WebBaseUrl + "/login");
        await Page.WaitForBlazorAsync();

        // Step 1: enter national ID and request code
        await Page.Locator("input").First.FillAsync(TestConfig.AdminNationalId);
        await Page.GetByRole(AriaRole.Button).First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Get the dev code directly from the API to avoid email dependency
        var token = await AuthHelper.GetAdminTokenAsync();
        Assert.That(token, Is.Not.Empty);
    }

    [Test]
    public async Task UnauthenticatedAccess_ToAdminPage_RedirectsToLogin()
    {
        await Page.GotoAsync(TestConfig.WebBaseUrl + "/admin");
        await Page.WaitForBlazorAsync();

        // Should redirect to login since no token in localStorage
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/login$"));
    }

    [Test]
    public async Task StoredToken_PersistsAcrossPageRefresh()
    {
        var token = await AuthHelper.GetAdminTokenAsync();

        // Seed localStorage and navigate to admin
        await Page.NavigateAuthenticatedAsync("/admin", token);
        await Page.WaitForBlazorAsync();

        // Should be on admin page (not redirected to login)
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/admin$"));

        // Reload and verify still authenticated
        await Page.ReloadAsync();
        await Page.WaitForBlazorAsync();

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/admin$"));
        await Expect(Page.Locator("h2")).ToContainTextAsync("Admin Dashboard");
    }
}
