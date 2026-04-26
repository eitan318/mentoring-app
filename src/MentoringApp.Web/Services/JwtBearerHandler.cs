using System.Net.Http.Headers;

namespace MentoringApp.Web.Services;

public class JwtBearerHandler(AuthStateService auth) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (auth.Token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return base.SendAsync(request, cancellationToken);
    }
}
