using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Models
{
    /// <summary>
    /// A one-shot signal that any number of callers can await.
    ///
    /// GO Map cannot convert between coordinates and world positions until its map
    /// origin has been set from the first GPS fix, so callers need a way to wait for
    /// that moment. Completing more than once is harmless.
    /// </summary>
    public class OriginSignal
    {
        private readonly TaskCompletionSource<bool> _source =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Completes once the origin has been set.</summary>
        public Task Task => _source.Task;

        public bool IsComplete => _source.Task.IsCompleted;

        public void Complete() => _source.TrySetResult(true);
    }
}
