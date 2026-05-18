using MentoringApp.ApiClient.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace MentoringApp.ApiClient.Extensions;

public static class ApiClientServiceExtensions
{
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
