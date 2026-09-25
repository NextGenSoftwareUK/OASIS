using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.ONET.Runtime;
using NextGenSoftware.OASIS.Edge.Runtime;
using NextGenSoftware.OASIS.ONET;
using Xunit;

namespace NextGenSoftware.OASIS.Edge.Runtime.UnitTests;

public sealed class OASISEdgeOnetRuntimeTests
{
    [Fact]
    public async Task StartBindsMatchingIdentityThenSynchronizesOverSharedOnetProtocol()
    {
        using var signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] publicKey = signingKey.ExportSubjectPublicKeyInfo();
        string nodeId = HyperDrivePeerBindingProof.DeriveNodeId(publicKey);
        using var hostSigningKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] hostPublicKey = hostSigningKey.ExportSubjectPublicKeyInfo();
        string hostNodeId = HyperDrivePeerBindingProof.DeriveNodeId(hostPublicKey);
        var edgeChannel = new LoopbackChannel(nodeId);
        var hostChannel = new LoopbackChannel(hostNodeId);
        edgeChannel.Peer = hostChannel;
        hostChannel.Peer = edgeChannel;
        using var hostEndpoint = new ONETRequestResponseEndpoint(hostChannel);
        var capabilityRegistry = new ONETCapabilityRegistry(hostEndpoint);
        var hostAdvertisement = await ONETCapabilityProof.CreateAsync(hostNodeId,
            Convert.ToBase64String(hostPublicKey), ONETNodeProfile.Full, new[] { "hyperdrive-sync-v3" },
            Array.Empty<ONETProviderCapability>(), DateTime.UtcNow, TimeSpan.FromMinutes(10),
            (message, _) => Task.FromResult(new OASISResult<string>(Convert.ToBase64String(
                hostSigningKey.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256)))), default);
        Assert.False(capabilityRegistry.RegisterLocal(hostAdvertisement.Result, hostNodeId).IsError);
        int exchanges = 0;
        hostEndpoint.RegisterHandler(ONETHyperDriveSyncTransport.OperationName, (context, _) =>
        {
            exchanges++;
            var request = JsonSerializer.Deserialize<SyncExchangeRequest>(context.PayloadJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.NotNull(request);
            return Task.FromResult(new OASISResult<string>
            {
                Result = JsonSerializer.Serialize(new SyncExchangeResponse
                {
                    ProtocolVersion = HyperDriveSyncProtocol.CurrentVersion,
                    OperationResults = Array.Empty<SyncOperationResult>(),
                    RemoteChanges = Array.Empty<SyncRemoteChange>(),
                    NextPullCheckpoint = "checkpoint-1"
                }, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            });
        });
        var binder = new RecordingPeerBinder();
        string databasePath = Path.Combine(Path.GetTempPath(), $"oasis-edge-onet-{Guid.NewGuid():N}.db");
        try
        {
            using var runtime = new OASISEdgeOnetRuntime(new EdgeRuntimeOptions
            {
                AvatarId = Guid.NewGuid(), DeviceId = Guid.NewGuid(), DatabasePath = databasePath
            }, edgeChannel, hostNodeId, binder, capabilityLifetime: TimeSpan.FromMilliseconds(250),
                capabilityRenewalInterval: TimeSpan.FromMilliseconds(50));

            var started = await runtime.StartAsync(new TestIdentity(nodeId, Convert.ToBase64String(publicKey), signingKey),
                new OnlineMonitor(), default);

            Assert.False(started.IsError, started.Message);
            Assert.Equal(1, binder.CallCount);
            Assert.Equal(1, exchanges);
            Assert.True(capabilityRegistry.TryGetCurrent(nodeId, out var advertised));
            Assert.Contains(advertised.Providers, x => x.ProviderType == "EdgeSQLiteOASIS");
            Assert.Equal(EdgeSynchronizationState.Synchronized, runtime.EdgeRuntime.Status.Synchronization);
            await Task.Delay(140);
            Assert.True(edgeChannel.CapabilityPublishCount >= 2);
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task StartRejectsIdentityDifferentFromAuthenticatedChannelBeforeBinding()
    {
        var binder = new RecordingPeerBinder();
        string databasePath = Path.Combine(Path.GetTempPath(), $"oasis-edge-onet-{Guid.NewGuid():N}.db");
        try
        {
            using var runtime = new OASISEdgeOnetRuntime(new EdgeRuntimeOptions
            {
                AvatarId = Guid.NewGuid(), DeviceId = Guid.NewGuid(), DatabasePath = databasePath
            }, new LoopbackChannel("channel-node"), "host", binder);

            var result = await runtime.StartAsync(new TestIdentity("different-node", "AQID", null),
                new OnlineMonitor(), default);

            Assert.True(result.IsError);
            Assert.Equal("EDGE_ONET_IDENTITY_MISMATCH", result.ErrorCode);
            Assert.Equal(0, binder.CallCount);
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task HostedOutageStillStartsRecoveryLifecycleAndCapabilityRenewal()
    {
        using var signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] publicKey = signingKey.ExportSubjectPublicKeyInfo();
        string nodeId = HyperDrivePeerBindingProof.DeriveNodeId(publicKey);
        using var hostSigningKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] hostPublicKey = hostSigningKey.ExportSubjectPublicKeyInfo();
        string hostNodeId = HyperDrivePeerBindingProof.DeriveNodeId(hostPublicKey);
        var edgeChannel = new LoopbackChannel(nodeId);
        var hostChannel = new LoopbackChannel(hostNodeId);
        edgeChannel.Peer = hostChannel;
        hostChannel.Peer = edgeChannel;
        using var hostEndpoint = new ONETRequestResponseEndpoint(hostChannel);
        var capabilityRegistry = new ONETCapabilityRegistry(hostEndpoint);
        var hostAdvertisement = await ONETCapabilityProof.CreateAsync(hostNodeId,
            Convert.ToBase64String(hostPublicKey), ONETNodeProfile.Full, new[] { "hyperdrive-sync-v3" },
            Array.Empty<ONETProviderCapability>(), DateTime.UtcNow, TimeSpan.FromMinutes(10),
            (message, _) => Task.FromResult(new OASISResult<string>(Convert.ToBase64String(
                hostSigningKey.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256)))), default);
        Assert.False(capabilityRegistry.RegisterLocal(hostAdvertisement.Result, hostNodeId).IsError);
        hostEndpoint.RegisterHandler(ONETHyperDriveSyncTransport.OperationName, (_, _) =>
            Task.FromResult(new OASISResult<string>
            {
                IsError = true, ErrorCount = 1, ErrorCode = "HYPERDRIVE_REMOTE_UNAVAILABLE",
                Message = "Hosted synchronization is temporarily unavailable."
            }));
        string databasePath = Path.Combine(Path.GetTempPath(), $"oasis-edge-onet-outage-{Guid.NewGuid():N}.db");
        try
        {
            await using var runtime = new OASISEdgeOnetRuntime(new EdgeRuntimeOptions
            {
                AvatarId = Guid.NewGuid(), DeviceId = Guid.NewGuid(), DatabasePath = databasePath,
                HostedServiceRecoveryInterval = TimeSpan.FromMilliseconds(20),
                MaximumHostedServiceRecoveryInterval = TimeSpan.FromMilliseconds(40)
            }, edgeChannel, hostNodeId, new RecordingPeerBinder(),
                capabilityLifetime: TimeSpan.FromMilliseconds(200),
                capabilityRenewalInterval: TimeSpan.FromMilliseconds(40));
            var identity = new TestIdentity(nodeId, Convert.ToBase64String(publicKey), signingKey);

            var started = await runtime.StartAsync(identity, new OnlineMonitor(), default);
            var duplicateStart = await runtime.StartAsync(identity, new OnlineMonitor(), default);
            await Task.Delay(110);

            Assert.True(started.IsError);
            Assert.NotEqual(EdgeConnectivityState.Online, runtime.EdgeRuntime.Status.Connectivity);
            Assert.Equal("EDGE_ONET_ALREADY_STARTED", duplicateStart.ErrorCode);
            Assert.True(edgeChannel.CapabilityPublishCount >= 2);
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                var file = databasePath + suffix;
                if (File.Exists(file)) File.Delete(file);
            }
        }
    }

    private sealed class LoopbackChannel : IONETApplicationMessageChannel
    {
        public LoopbackChannel(string localNodeId) => LocalNodeId = localNodeId;
        public string LocalNodeId { get; }
        public LoopbackChannel? Peer { get; set; }
        public int CapabilityPublishCount { get; private set; }
        public event EventHandler<ONETApplicationMessage>? MessageReceived;
        public Task<OASISResult<bool>> SendAsync(ONETApplicationMessage message, CancellationToken cancellationToken)
        {
            if (message.Envelope.Operation == ONETCapabilityPublisher.OperationName) CapabilityPublishCount++;
            if (Peer == null)
                return Task.FromResult(new OASISResult<bool>
                { IsError = true, ErrorCount = 1, ErrorCode = "ONET_CHANNEL_UNAVAILABLE", Message = "No peer." });
            Peer.MessageReceived?.Invoke(Peer, message);
            return Task.FromResult(new OASISResult<bool>(true));
        }
    }

    private sealed class RecordingPeerBinder : IHyperDrivePeerBindingTransport
    {
        public int CallCount { get; private set; }
        public Task<OASISResult<bool>> BindPeerAsync(BindHyperDrivePeerRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new OASISResult<bool>(true));
        }
    }

    private sealed class TestIdentity : IEdgeNodeIdentity
    {
        private readonly ECDsa? _key;
        public TestIdentity(string nodeId, string publicKey, ECDsa? key) { NodeId = nodeId; PublicKey = publicKey; _key = key; }
        public string NodeId { get; }
        public string PublicKey { get; }
        public Task<OASISResult<string>> SignAsync(string message, CancellationToken cancellationToken) =>
            Task.FromResult(_key == null
                ? new OASISResult<string> { IsError = true, ErrorCount = 1, ErrorCode = "NO_KEY" }
                : new OASISResult<string>(Convert.ToBase64String(
                    _key.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256))));
    }

    private sealed class OnlineMonitor : IEdgeConnectivityMonitor
    {
        public bool IsOnline => true;
        public event EventHandler<EdgeConnectivityChangedEventArgs>? ConnectivityChanged;
    }
}
