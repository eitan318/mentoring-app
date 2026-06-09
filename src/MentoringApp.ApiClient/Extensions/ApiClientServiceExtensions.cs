using MentoringApp.ApiClient.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace MentoringApp.ApiClient.Extensions;

/// <summary>
/// DI registration for the typed API clients. Registers each client with the given base URL and
/// attaches the retry handler (and, in the auth overload, a Bearer-token handler) to its pipeline.
/// </summary>
public static class ApiClientServiceExtensions
{
    /// <summary>Registers all API clients with retry only (no auth) — used by anonymous flows.</summary>
    public static IServiceCollection AddApiClients(this IServiceCollection services, string baseUrl)
    {
        services.AddTransient<SimpleRetryHandler>();

        services.AddHttpClient<AuthApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>();
        services.AddHttpClient<UserApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>();
        services.AddHttpClient<PairApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>();
        services.AddHttpClient<MatchingApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>();
        services.AddHttpClient<IssueApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>();
        services.AddHttpClient<ReviewApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>();
        services.AddHttpClient<ReferenceApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>();
        services.AddHttpClient<SettingsApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>();
        services.AddHttpClient<NotificationApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>();
        return services;
    }

    /// <summary>Registers all API clients with a Bearer token DelegatingHandler.</summary>
    public static IServiceCollection AddApiClientsWithAuth<THandler>(
        this IServiceCollection services, string baseUrl)
        where THandler : DelegatingHandler
    {
        services.AddTransient<THandler>();
        services.AddTransient<SimpleRetryHandler>();

        services.AddHttpClient<AuthApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>();
        services.AddHttpClient<UserApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>()
            .AddHttpMessageHandler<THandler>();
        services.AddHttpClient<PairApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>()
            .AddHttpMessageHandler<THandler>();
        services.AddHttpClient<MatchingApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>()
            .AddHttpMessageHandler<THandler>();
        services.AddHttpClient<IssueApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>()
            .AddHttpMessageHandler<THandler>();
        services.AddHttpClient<ReviewApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>()
            .AddHttpMessageHandler<THandler>();
        services.AddHttpClient<ReferenceApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>()
            .AddHttpMessageHandler<THandler>();
        services.AddHttpClient<SettingsApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>()
            .AddHttpMessageHandler<THandler>();
        services.AddHttpClient<NotificationApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<SimpleRetryHandler>()
            .AddHttpMessageHandler<THandler>();
        return services;
    }
}
