using System.Net.Http.Headers;
using MentoringApp.Components.Auth;

namespace MentoringApp.Web.Services;

public class BearerTokenHandler(IAuthService authService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        if (authService.Token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authService.Token);

        return await base.SendAsync(request, ct);
    }
}
