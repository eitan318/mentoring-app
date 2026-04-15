using MentoringApp.ApiClient.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace MentoringApp.ApiClient.Extensions;

public static class ApiClientServiceExtensions
{
    public static IServiceCollection AddApiClients(this IServiceCollection services, string baseUrl)
    {
        services.AddHttpClient<AuthApiClient>(c => c.BaseAddress = new Uri(baseUrl));
        services.AddHttpClient<UserApiClient>(c => c.BaseAddress = new Uri(baseUrl));
        services.AddHttpClient<PairApiClient>(c => c.BaseAddress = new Uri(baseUrl));
        services.AddHttpClient<MatchingApiClient>(c => c.BaseAddress = new Uri(baseUrl));
        services.AddHttpClient<IssueApiClient>(c => c.BaseAddress = new Uri(baseUrl));
        services.AddHttpClient<ReviewApiClient>(c => c.BaseAddress = new Uri(baseUrl));
        services.AddHttpClient<ReferenceApiClient>(c => c.BaseAddress = new Uri(baseUrl));
        return services;
    }
}
