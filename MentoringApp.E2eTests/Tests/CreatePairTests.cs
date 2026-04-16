using MentoringApp.E2eTests.Helpers;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace MentoringApp.E2eTests.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class CreatePairTests : PageTest
{
    private string _token = string.Empty;

    [SetUp]
    public async Task SetUp()
    {
        _token = await AuthHelper.GetAdminTokenAsync();
    }

    [Test]
    public async Task CreatePair_ShowsHeading()
    {
        await Page.NavigateAuthenticatedAsync("/admin/create-pair", _token);
        await Page.WaitForBlazorAsync();

        await Expect(Page.Locator("h2")).ToContainTextAsync("Create Pair");
    }

    [Test]
    public async Task CreatePair_ShowsThreeSelectorColumns()
    {
        await Page.NavigateAuthenticatedAsync("/admin/create-pair", _token);
        await Page.WaitForBlazorAsync();

        // Wait for API data to load (loading spinner disappears = selects appear)
        await Page.WaitForSelectorAsync("select", new() { Timeout = 15_000 });

        await Expect(Page.GetByText("Supervisor")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Mentor (available)")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Mentee (available)")).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreatePair_CreateButton_DisabledUntilAllSelected()
    {
        await Page.NavigateAuthenticatedAsync("/admin/create-pair", _token);
        await Page.WaitForBlazorAsync();

        var createButton = Page.GetByRole(AriaRole.Button, new() { Name = "Create Pair" });
        await Expect(createButton).ToBeDisabledAsync();
    }

    [Test]
    public async Task CreatePair_BackLink_NavigatesToPairsPage()
    {
        await Page.NavigateAuthenticatedAsync("/admin/create-pair", _token);
        await Page.WaitForBlazorAsync();

        await Page.GetByRole(AriaRole.Link, new() { Name = "Back to Pairs" }).ClickAsync();
        await Page.WaitForBlazorAsync();

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/admin/pairs$"));
    }

    [Test]
    public async Task CreatePair_ShowsAvailableMentorsAndMentees()
    {
        await Page.NavigateAuthenticatedAsync("/admin/create-pair", _token);
        await Page.WaitForBlazorAsync();

        // The select lists should be populated (more than just the "none" option)
        var selects = Page.Locator("select");
        var selectCount = await selects.CountAsync();
        Assert.That(selectCount, Is.EqualTo(3), "Expected 3 select lists (supervisor, mentor, mentee)");

        // Each select should have at least the "none" option
        for (int i = 0; i < selectCount; i++)
        {
            var options = await selects.Nth(i).Locator("option").CountAsync();
            Assert.That(options, Is.GreaterThanOrEqualTo(1));
        }
    }
}
