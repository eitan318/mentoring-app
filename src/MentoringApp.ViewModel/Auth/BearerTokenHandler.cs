using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MentoringApp.ViewModel.Auth;

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
