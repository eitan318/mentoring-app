using MentoringApp.E2eTests.Helpers;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace MentoringApp.E2eTests.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class ManagePairsTests : PageTest
{
    private string _token = string.Empty;

    [SetUp]
    public async Task SetUp()
    {
        _token = await AuthHelper.GetAdminTokenAsync();
    }

    [Test]
    public async Task ManagePairs_ShowsHeading()
    {
        await Page.NavigateAuthenticatedAsync("/admin/pairs", _token);
        await Page.WaitForBlazorAsync();

        await Expect(Page.Locator("h2")).ToContainTextAsync("Manage Pairs");
    }

    [Test]
    public async Task ManagePairs_ShowsCreatePairButton()
    {
        await Page.NavigateAuthenticatedAsync("/admin/pairs", _token);
        await Page.WaitForBlazorAsync();

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "+ Create Pair" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ManagePairs_ShowsPairsTable_WithNameColumns()
    {
        await Page.NavigateAuthenticatedAsync("/admin/pairs", _token);
        await Page.WaitForBlazorAsync();

        // Wait for the table to finish loading (header cells appear after API call)
        await Page.WaitForSelectorAsync("th", new() { Timeout = 15_000 });

        await Expect(Page.GetByRole(AriaRole.Columnheader, new() { Name = "Mentor" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Columnheader, new() { Name = "Mentee" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Columnheader, new() { Name = "Supervisor" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ManagePairs_SearchFilter_Works()
    {
        await Page.NavigateAuthenticatedAsync("/admin/pairs", _token);
        await Page.WaitForBlazorAsync();

        var searchInput = Page.Locator("input[placeholder*='Search']");
        await Expect(searchInput).ToBeVisibleAsync();

        await searchInput.FillAsync("zzz_no_match_zzz");
        // After filtering with no-match text, table should show no data rows or empty state
        var rows = await Page.Locator("tbody tr").CountAsync();
        Assert.That(rows, Is.EqualTo(0), "Expected no rows after searching for non-existent name");
    }

    [Test]
    public async Task ManagePairs_SeparateButton_ShowsConfirmation()
    {
        await Page.NavigateAuthenticatedAsync("/admin/pairs", _token);
        await Page.WaitForBlazorAsync();

        var separateButtons = Page.GetByRole(AriaRole.Button, new() { Name = "Separate" });
        var count = await separateButtons.CountAsync();

        if (count == 0)
        {
            Assert.Ignore("No pairs exist to test separation.");
        }

        await separateButtons.First.ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Yes" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "No" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ManagePairs_CreatePairButton_NavigatesToCreatePairPage()
    {
        await Page.NavigateAuthenticatedAsync("/admin/pairs", _token);
        await Page.WaitForBlazorAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "+ Create Pair" }).ClickAsync();
        await Page.WaitForBlazorAsync();

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/admin/create-pair$"));
    }
}
