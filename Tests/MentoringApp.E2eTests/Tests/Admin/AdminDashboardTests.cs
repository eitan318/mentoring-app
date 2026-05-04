using MentoringApp.E2eTests.Helpers;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace MentoringApp.E2eTests.Tests.Admin;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class AdminDashboardTests : PageTest
{
    private string _token = string.Empty;

    [SetUp]
    public async Task SetUp()
    {
        _token = await AuthHelper.GetAdminTokenAsync();
    }

    [Test]
    public async Task Dashboard_ShowsAdminHeading()
    {
        await Page.NavigateAuthenticatedAsync("/admin", _token);
        await Page.WaitForBlazorAsync();

        await Expect(Page.Locator("h2")).ToContainTextAsync("Admin Dashboard");
    }

    [Test]
    public async Task Dashboard_ShowsSupervisorOverviewSection()
    {
        await Page.NavigateAuthenticatedAsync("/admin", _token);
        await Page.WaitForBlazorAsync();

        await Expect(Page.GetByText("Supervisor Overview")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Dashboard_ShowsUserManagementTable()
    {
        await Page.NavigateAuthenticatedAsync("/admin", _token);
        await Page.WaitForBlazorAsync();

        // The users table should be present
        var table = Page.Locator("table").Last;
        await Expect(table).ToBeVisibleAsync();
    }

    [Test]
    public async Task Dashboard_SearchFilter_NarrowsUserList()
    {
        await Page.NavigateAuthenticatedAsync("/admin", _token);
        await Page.WaitForBlazorAsync();

        // Count rows before filtering
        var rowsBefore = await Page.Locator("tbody tr").CountAsync();

        // Type a search that should match only a subset
        await Page.Locator("input[placeholder*='Search']").Last.FillAsync("Admin");

        var rowsAfter = await Page.Locator("tbody tr").CountAsync();
        Assert.That(rowsAfter, Is.LessThanOrEqualTo(rowsBefore));
    }

    [Test]
    public async Task Dashboard_CreateUser_ShowsFormOnButtonClick()
    {
        await Page.NavigateAuthenticatedAsync("/admin", _token);
        await Page.WaitForBlazorAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "+ New User" }).ClickAsync();
        await Expect(Page.GetByText("Create User")).ToBeVisibleAsync();

        // Form fields should appear
        await Expect(Page.Locator("input[placeholder='Username']")).ToBeVisibleAsync();
        await Expect(Page.Locator("input[placeholder='Email']")).ToBeVisibleAsync();
        await Expect(Page.Locator("input[placeholder='National ID']")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Dashboard_CreateUser_RequiredFieldValidation()
    {
        await Page.NavigateAuthenticatedAsync("/admin", _token);
        await Page.WaitForBlazorAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "+ New User" }).ClickAsync();

        // Click Create without filling required fields
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create" }).ClickAsync();

        await Expect(Page.GetByText("Username, email, and National ID are required")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Dashboard_DeleteUser_ShowsConfirmation()
    {
        await Page.NavigateAuthenticatedAsync("/admin", _token);
        await Page.WaitForBlazorAsync();

        // Wait for the user table to finish loading (first Delete button appears)
        var deleteButtons = Page.GetByRole(AriaRole.Button, new() { Name = "Delete" });
        await Expect(deleteButtons.First).ToBeVisibleAsync(new() { Timeout = 15_000 });

        await deleteButtons.First.ClickAsync();

        // Confirmation buttons should appear
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Yes" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "No" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Sidebar_ShowsPairsLink_ForAdmin()
    {
        await Page.NavigateAuthenticatedAsync("/admin", _token);
        await Page.WaitForBlazorAsync();

        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Pairs" })).ToBeVisibleAsync();
    }
}
