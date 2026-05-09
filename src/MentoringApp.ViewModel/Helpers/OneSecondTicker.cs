using System;
using System.Threading;
using System.Threading.Tasks;

namespace MentoringApp.ViewModel.Helpers
{
    /// <summary>
    /// Wraps a one-second PeriodicTimer with start/stop semantics. Used by
    /// dashboard view models to drive countdown displays without duplicating
    /// the CancellationTokenSource + PeriodicTimer scaffolding.
    /// </summary>
    public sealed class OneSecondTicker : IDisposable
    {
        private readonly Action _onTick;
        private CancellationTokenSource? _cts;

        public OneSecondTicker(Action onTick) => _onTick = onTick ?? throw new ArgumentNullException(nameof(onTick));

        /// <summary>Starts ticking. If already running, restarts the timer.</summary>
        public void Start()
        {
            Stop();
            _cts = new CancellationTokenSource();
            _ = RunAsync(_cts.Token);
        }

        public void Stop() => _cts?.Cancel();

        public void Dispose() => Stop();

        private async Task RunAsync(CancellationToken token)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            try
            {
                while (await timer.WaitForNextTickAsync(token))
                    _onTick();
            }
            catch (OperationCanceledException) { }
        }
    }
}
