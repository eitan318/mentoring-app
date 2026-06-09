using System.Net.Http;

namespace MentoringApp.ApiClient;

/// <summary>
/// HttpClient pipeline handler that transparently retries a request up to 3 times (2s apart)
/// when the network fails (connection refused / socket errors), e.g. while the API is still starting up.
/// Registered on every API client in <see cref="Extensions.ApiClientServiceExtensions"/>.
/// </summary>
public class SimpleRetryHandler : DelegatingHandler
{
    private readonly int _maxRetries = 3;
    private readonly TimeSpan _delay = TimeSpan.FromSeconds(2);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (int i = 0; i < _maxRetries; i++)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException) when (i < _maxRetries - 1)
            {
                await Task.Delay(_delay, cancellationToken);
            }
            catch (System.Net.Sockets.SocketException) when (i < _maxRetries - 1)
            {
                await Task.Delay(_delay, cancellationToken);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
