using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MentoringApp.ViewModel.Auth;

/// <summary>
/// HttpClient pipeline handler that attaches the current JWT (from <see cref="AuthTokenStore"/>)
/// as a Bearer header on every request, and triggers session-expiry handling on a 401 response.
/// Registered on the authenticated API clients.
/// </summary>
public class BearerTokenHandler(AuthTokenStore authTokenStore) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var wasAuthenticated = authTokenStore.IsAuthenticated;
        if (authTokenStore.Token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authTokenStore.Token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && wasAuthenticated)
            authTokenStore.NotifySessionExpired();

        return response;
    }
}
