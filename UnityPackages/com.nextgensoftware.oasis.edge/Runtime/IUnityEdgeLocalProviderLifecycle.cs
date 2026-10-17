using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.Unity
{
    /// <summary>
    /// Owns an optional local provider used by the Unity Edge host. The host starts this provider
    /// before OGEngineClient, suspends it after the Edge workers, and resumes it before Edge sync.
    /// </summary>
    public interface IUnityEdgeLocalProviderLifecycle : IAsyncDisposable
    {
        IHyperDriveLocalReplicationTarget ReplicationTarget { get; }
        Task<OASISResult<bool>> StartAsync(CancellationToken cancellationToken = default);
        Task<OASISResult<bool>> SuspendAsync(CancellationToken cancellationToken = default);
        Task<OASISResult<bool>> ResumeAsync(CancellationToken cancellationToken = default);
    }
}
