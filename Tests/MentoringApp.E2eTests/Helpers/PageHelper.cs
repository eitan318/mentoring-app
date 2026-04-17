using Microsoft.Playwright;

namespace MentoringApp.E2eTests.Helpers;

/// <summary>
/// Convenience extensions for Playwright IPage used across tests.
/// </summary>
public static class PageHelper
{
    /// <summary>
    /// Seeds the localStorage JWT so the page starts in an authenticated state,
    /// then navigates to <paramref name="path"/>.
    /// </summary>
    public static async Task NavigateAuthenticatedAsync(this IPage page, string path, string token)
    {
        // Navigate to the origin first so localStorage is available for that origin.
        await page.GotoAsync(TestConfig.WebBaseUrl);
        await page.EvaluateAsync(
            "([key, val]) => localStorage.setItem(key, val)",
            new[] { TestConfig.LocalStorageKey, token });
        await page.GotoAsync(TestConfig.WebBaseUrl + path);
    }

    /// <summary>
    /// Waits for the Blazor WASM app to fully render.
    /// Polls until the sidebar (rendered by MainLayout after auth init) appears,
    /// or the login page renders — whichever comes first.
    /// </summary>
    public static async Task WaitForBlazorAsync(this IPage page)
    {
        // Wait for load state first, then poll for a rendered Blazor element.
        await page.WaitForLoadStateAsync(LoadState.Load);
        await page.WaitForSelectorAsync("nav.sidebar, h2", new() { Timeout = 15_000 });
    }
}
