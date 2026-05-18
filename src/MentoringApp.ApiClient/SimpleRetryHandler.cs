using System.Net.Http;

namespace MentoringApp.ApiClient;

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
