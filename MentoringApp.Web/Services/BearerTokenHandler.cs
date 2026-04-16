using System.Net.Http.Headers;
using MentoringApp.Components.Auth;

namespace MentoringApp.Web.Services;

/// <summary>
/// Attaches the Bearer token to every outgoing API request.
/// Injects the singleton <see cref="AuthState"/> directly — not the scoped
/// <see cref="IAuthService"/> — to avoid DI captive-dependency issues with
/// IHttpClientFactory-managed handler lifetimes.
/// </summary>
public class BearerTokenHandler(AuthState state) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        if (state.Token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", state.Token);

        return base.SendAsync(request, ct);
    }
}
