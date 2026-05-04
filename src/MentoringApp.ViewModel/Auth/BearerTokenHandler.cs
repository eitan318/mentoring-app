using System.Net.Http;
using System.Net.Http.Headers;

namespace MentoringApp.ViewModel.Auth;

public class BearerTokenHandler(AuthTokenStore authTokenStore) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (authTokenStore.Token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authTokenStore.Token);
        return base.SendAsync(request, cancellationToken);
    }
}
