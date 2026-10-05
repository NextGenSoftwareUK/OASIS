using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.Runtime.Holochain;
using UnityEngine;

namespace NextGenSoftware.OASIS.Edge.Unity.Holochain
{
    /// <summary>Holo-enabled implementation of Unity's optional Edge local-provider lifecycle.</summary>
    public sealed class HoloEdgeUnityLocalProvider : IUnityEdgeLocalProviderLifecycle
    {
        private readonly HoloEdgeRuntimeHost _host;

        public IHyperDriveLocalReplicationTarget ReplicationTarget => _host.Repository;

        public HoloEdgeUnityLocalProvider(HolochainAndroidRuntimeOptions options)
            : this(new UnityHolochainAndroidServiceBridge(), options)
        {
        }

        internal HoloEdgeUnityLocalProvider(IHolochainAndroidServiceBridge bridge,
            HolochainAndroidRuntimeOptions options, IHoloEdgeSessionFactory sessionFactory = null) =>
            _host = new HoloEdgeRuntimeHost(bridge ?? throw new ArgumentNullException(nameof(bridge)),
                options ?? throw new ArgumentNullException(nameof(options)), sessionFactory);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterFactory() => UnityEdgeLocalProviderFactory.Register("holo",
            options =>
            {
                if (options.AppBundle == null || options.AppBundle.Length == 0)
                    return ErrorLifecycle("HOLO_EDGE_APP_BUNDLE_REQUIRED", "The packaged Holochain hApp is required.");
                return new OASISResult<IUnityEdgeLocalProviderLifecycle>(new HoloEdgeUnityLocalProvider(
                    new HolochainAndroidRuntimeOptions
                    {
                        AppBundle = options.AppBundle,
                        InstalledAppId = string.IsNullOrWhiteSpace(options.InstalledAppId) ? "oasis" : options.InstalledAppId,
                        NetworkSeed = options.NetworkSeed,
                        BootstrapUrl = string.IsNullOrWhiteSpace(options.BootstrapUrl)
                            ? "https://dev-test-bootstrap2.holochain.org" : options.BootstrapUrl,
                        RelayUrl = string.IsNullOrWhiteSpace(options.RelayUrl)
                            ? "https://use1-1.relay.n0.iroh-canary.iroh.link./" : options.RelayUrl
                    }));
            });

        public async Task<OASISResult<bool>> StartAsync(CancellationToken cancellationToken = default)
        {
            var started = await _host.StartAsync(cancellationToken).ConfigureAwait(false);
            return started == null || started.IsError || started.Result == null
                ? Error(started?.ErrorCode ?? "HOLO_EDGE_START_FAILED",
                    started?.Message ?? "The Holo Edge host returned no repository.")
                : new OASISResult<bool>(true);
        }

        public async Task<OASISResult<bool>> SuspendAsync(CancellationToken cancellationToken = default) =>
            await _host.SuspendAsync(cancellationToken).ConfigureAwait(false);

        public async Task<OASISResult<bool>> ResumeAsync(CancellationToken cancellationToken = default)
        {
            var resumed = await _host.ResumeAsync(cancellationToken).ConfigureAwait(false);
            return resumed == null || resumed.IsError || resumed.Result == null
                ? Error(resumed?.ErrorCode ?? "HOLO_EDGE_RESUME_FAILED",
                    resumed?.Message ?? "The Holo Edge host returned no resumed repository.")
                : new OASISResult<bool>(true);
        }

        public ValueTask DisposeAsync() => _host.DisposeAsync();

        private static OASISResult<bool> Error(string code, string message) => new OASISResult<bool>
        {
            IsError = true, ErrorCount = 1, ErrorCode = code, Message = message
        };

        private static OASISResult<IUnityEdgeLocalProviderLifecycle> ErrorLifecycle(string code, string message) =>
            new OASISResult<IUnityEdgeLocalProviderLifecycle>
            { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
    }
}
