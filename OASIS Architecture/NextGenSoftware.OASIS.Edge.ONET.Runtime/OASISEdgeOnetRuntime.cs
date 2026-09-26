using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.Runtime;
using NextGenSoftware.OASIS.ONET;

namespace NextGenSoftware.OASIS.Edge.ONET.Runtime
{
    /// <summary>
    /// Edge ONODE composition root. The host supplies an authenticated ONET channel and the HTTPS
    /// bootstrap transports used to bind the device identity and obtain offline grants. No Full ONODE,
    /// BootLoader, ASP.NET or server provider dependency is loaded on the device.
    /// </summary>
    public sealed class OASISEdgeOnetRuntime : IDisposable, IAsyncDisposable
    {
        private readonly ONETRequestResponseEndpoint _endpoint;
        private readonly IONETCapabilityPublisher _capabilityPublisher;
        private readonly IReadOnlyList<ONETProviderCapability> _eligibleProviderCapabilities;
        private readonly TimeSpan _capabilityLifetime;
        private readonly TimeSpan _capabilityRenewalInterval;
        private readonly CancellationTokenSource _capabilityRenewalCancellation = new CancellationTokenSource();
        private Task _capabilityRenewalTask;
        private bool _started;
        private bool _disposed;

        public OASISEdgeRuntime EdgeRuntime { get; }
        public string LocalNodeId { get; }
        public string HostedNodeId { get; }
        public string LastCapabilityAdvertisementError { get; private set; }
        public string LastCapabilityAdvertisementWarning { get; private set; }

        public OASISEdgeOnetRuntime(EdgeRuntimeOptions options,
            IONETApplicationMessageChannel channel, string hostedNodeId,
            IHyperDrivePeerBindingTransport peerBindingTransport,
            IHyperDriveOfflineSessionGrantTransport offlineGrantTransport = null,
            IEdgeSecureSessionStore secureSessionStore = null,
            IEdgeOfflineGrantValidator offlineGrantValidator = null,
            IHyperDriveClock clock = null,
            IEnumerable<ONETProviderCapability> eligibleProviderCapabilities = null,
            TimeSpan? capabilityLifetime = null,
            TimeSpan? capabilityRenewalInterval = null,
            IEnumerable<string> capabilityRegistryNodeIds = null,
            int? capabilityRegistryQuorum = null)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrWhiteSpace(hostedNodeId))
                throw new ArgumentException("A hosted ONODE identifier is required.", nameof(hostedNodeId));
            if (peerBindingTransport == null)
                throw new ArgumentNullException(nameof(peerBindingTransport),
                    "Edge ONET requires an authenticated bootstrap transport for durable identity binding.");
            LocalNodeId = channel.LocalNodeId;
            HostedNodeId = hostedNodeId;
            _endpoint = new ONETRequestResponseEndpoint(channel);
            var registryNodeIds = (capabilityRegistryNodeIds ?? new[] { hostedNodeId })
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())
                .Distinct(StringComparer.Ordinal).ToArray();
            if (registryNodeIds.Length == 0)
                throw new ArgumentException("At least one capability registry node is required.", nameof(capabilityRegistryNodeIds));
            int registryQuorum = capabilityRegistryQuorum ?? registryNodeIds.Length;
            if (registryQuorum < 1 || registryQuorum > registryNodeIds.Length)
                throw new ArgumentOutOfRangeException(nameof(capabilityRegistryQuorum));
            var publishers = registryNodeIds.Select(x => (IONETCapabilityPublisher)
                new ONETCapabilityPublisher(_endpoint, x)).ToArray();
            _capabilityPublisher = publishers.Length == 1 ? publishers[0] :
                new ONETFederatedCapabilityPublisher(publishers, registryQuorum);
            _capabilityLifetime = capabilityLifetime ?? TimeSpan.FromMinutes(10);
            _capabilityRenewalInterval = capabilityRenewalInterval ?? TimeSpan.FromMinutes(5);
            if (_capabilityLifetime <= TimeSpan.Zero || _capabilityRenewalInterval <= TimeSpan.Zero ||
                _capabilityRenewalInterval >= _capabilityLifetime)
                throw new ArgumentException("Capability renewal must be positive and occur before the signed lease expires.");
            _eligibleProviderCapabilities = new[]
            {
                new ONETProviderCapability
                {
                    ProviderType = "EdgeSQLiteOASIS", ProviderCategory = "Storage",
                    Capabilities = new[] { "durable-outbox", "local-read", "local-write", "snapshot-rebase" }
                }
            }.Concat(eligibleProviderCapabilities ?? Array.Empty<ONETProviderCapability>()).ToArray();
            var directories = registryNodeIds.Select(x => (IONETCapabilityDirectory)
                new ONETCapabilityDirectoryClient(_endpoint, x)).ToArray();
            IONETCapabilityDirectory capabilityDirectory = directories.Length == 1 ? directories[0] :
                new ONETFederatedCapabilityDirectory(directories, registryQuorum);
            var syncTransport = new ONETHyperDriveSyncTransport(_endpoint, capabilityDirectory,
                new ONETCapabilityQuery { Service = "hyperdrive-sync-v3", NodeProfile = ONETNodeProfile.Full });
            EdgeRuntime = new OASISEdgeRuntime(options, syncTransport, secureSessionStore,
                offlineGrantValidator, clock, peerBindingTransport, offlineGrantTransport);
        }

        public async Task<OASISResult<SyncCycleResult>> StartAsync(IEdgeNodeIdentity identity,
            IEdgeConnectivityMonitor connectivityMonitor, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (_started)
                return Error("EDGE_ONET_ALREADY_STARTED", "The Edge ONODE lifecycle has already started.");
            if (identity == null || !string.Equals(identity.NodeId, LocalNodeId, StringComparison.Ordinal))
                return Error("EDGE_ONET_IDENTITY_MISMATCH",
                    "The platform-secure identity must match the authenticated ONET channel identity.");
            if (connectivityMonitor == null)
                return Error("EDGE_CONNECTIVITY_MONITOR_REQUIRED", "A platform connectivity monitor is required.");

            var bound = await EdgeRuntime.BindOnetIdentityAsync(identity, cancellationToken).ConfigureAwait(false);
            if (bound == null || bound.IsError || !bound.Result)
                return Error(bound?.ErrorCode ?? "EDGE_ONET_BINDING_FAILED",
                    bound?.Message ?? "The hosted ONODE did not persist the peer binding.");

            var published = await PublishCapabilitiesAsync(identity, cancellationToken).ConfigureAwait(false);
            if (published == null || published.IsError || !published.Result)
                return Error(published?.ErrorCode ?? "EDGE_ONET_CAPABILITY_PUBLISH_FAILED",
                    published?.Message ?? "The Edge ONODE capability advertisement was not accepted.");
            LastCapabilityAdvertisementWarning = published.IsWarning ? published.Message : null;

            var started = await EdgeRuntime.StartConnectivityMonitoringAsync(connectivityMonitor,
                cancellationToken).ConfigureAwait(false);
            // Monitoring is attached before the initial synchronization attempt. A hosted outage may make
            // that first attempt fail, but the Edge lifecycle is still running locally and owns its recovery
            // loop. Mark it started and renew the signed advertisement while recovery is pending.
            _started = true;
            _capabilityRenewalTask = RenewCapabilitiesAsync(identity, _capabilityRenewalCancellation.Token);
            return started;
        }

        private async Task<OASISResult<bool>> PublishCapabilitiesAsync(IEdgeNodeIdentity identity,
            CancellationToken cancellationToken)
        {
            var advertisement = await ONETCapabilityProof.CreateAsync(identity.NodeId, identity.PublicKey,
                ONETNodeProfile.Edge, new[] { "hyperdrive-sync-v3", "offline-first" },
                _eligibleProviderCapabilities, DateTime.UtcNow, _capabilityLifetime, identity.SignAsync,
                cancellationToken).ConfigureAwait(false);
            if (advertisement == null || advertisement.IsError || advertisement.Result == null)
                return new OASISResult<bool> { IsError = true, ErrorCount = 1,
                    ErrorCode = advertisement?.ErrorCode ?? "EDGE_ONET_CAPABILITY_CREATE_FAILED",
                    Message = advertisement?.Message ?? "The Edge ONODE capability advertisement could not be created." };
            return await _capabilityPublisher.PublishAsync(advertisement.Result, cancellationToken).ConfigureAwait(false);
        }

        private async Task RenewCapabilitiesAsync(IEdgeNodeIdentity identity, CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    await Task.Delay(_capabilityRenewalInterval, cancellationToken).ConfigureAwait(false);
                    try
                    {
                        var renewed = await PublishCapabilitiesAsync(identity, cancellationToken).ConfigureAwait(false);
                        LastCapabilityAdvertisementError = renewed == null || renewed.IsError || !renewed.Result
                            ? renewed?.Message ?? "The capability registry returned no result." : null;
                        LastCapabilityAdvertisementWarning = renewed != null && renewed.IsWarning
                            ? renewed.Message : null;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                    catch (Exception ex) { LastCapabilityAdvertisementError = ex.Message; }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        }

        public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            _capabilityRenewalCancellation.Cancel();
            if (_capabilityRenewalTask != null)
            {
                try { await _capabilityRenewalTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }
            await EdgeRuntime.DisposeAsync().ConfigureAwait(false);
            _endpoint.Dispose();
            _capabilityRenewalCancellation.Dispose();
        }

        private void ThrowIfDisposed()
        { if (_disposed) throw new ObjectDisposedException(nameof(OASISEdgeOnetRuntime)); }
        private static OASISResult<SyncCycleResult> Error(string code, string message) =>
            new OASISResult<SyncCycleResult>
            { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
    }
}
