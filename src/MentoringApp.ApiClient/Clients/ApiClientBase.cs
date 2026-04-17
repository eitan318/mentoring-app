using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MentoringApp.ApiClient.Exceptions;

namespace MentoringApp.ApiClient.Clients;

public abstract class ApiClientBase(HttpClient http)
{
    protected readonly HttpClient Http = http;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected async Task<T> GetAsync<T>(string url)
    {
        var response = await Http.GetAsync(url);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    protected async Task<T> PostAsync<T>(string url, object? body = null)
    {
        var response = await Http.PostAsJsonAsync(url, body, JsonOptions);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    protected async Task PostAsync(string url, object? body = null)
    {
        var response = await Http.PostAsJsonAsync(url, body, JsonOptions);
        await EnsureSuccessAsync(response);
    }

    protected async Task PutAsync(string url, object? body = null)
    {
        var response = body is null
            ? await Http.PutAsync(url, null)
            : await Http.PutAsJsonAsync(url, body, JsonOptions);
        await EnsureSuccessAsync(response);
    }

    protected async Task<T> PutAsync<T>(string url, object? body = null)
    {
        var response = body is null
            ? await Http.PutAsync(url, null)
            : await Http.PutAsJsonAsync(url, body, JsonOptions);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    protected async Task DeleteAsync(string url)
    {
        var response = await Http.DeleteAsync(url);
        await EnsureSuccessAsync(response);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        string? errorMessage = null;
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorBody>(JsonOptions);
            errorMessage = body?.Error;
        }
        catch { /* ignore deserialization failures */ }

        throw new ApiException(
            errorMessage ?? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}",
            response.StatusCode);
    }

    private record ErrorBody(string? Error);
}
