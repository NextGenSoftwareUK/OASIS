using System.Collections.Concurrent;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.ONET;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests;

public class ONETRequestResponseTests
{
    [Fact]
    public async Task RequestAsync_CompletesOnlyWithCorrelatedResponseFromExpectedNode()
    {
        var clientChannel = new TestChannel("edge");
        var hostChannel = new TestChannel("host");
        clientChannel.Peer = hostChannel;
        hostChannel.Peer = clientChannel;
        using var client = new ONETRequestResponseEndpoint(clientChannel);
        using var host = new ONETRequestResponseEndpoint(hostChannel);
        host.RegisterHandler("echo", (request, _) => Task.FromResult(Success(request.PayloadJson)));

        var result = await client.RequestAsync("host", "echo", "payload", CancellationToken.None);

        Assert.False(result.IsError, result.Message);
        Assert.Equal("payload", result.Result);
    }

    [Fact]
    public async Task RequestAsync_DeliveryWithoutResponse_DoesNotReportSuccess()
    {
        var channel = new TestChannel("edge") { DeliverMessages = false };
        using var endpoint = new ONETRequestResponseEndpoint(channel);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var result = await endpoint.RequestAsync("host", "sync", "{}", cancellation.Token);

        Assert.True(result.IsError);
        Assert.Equal("ONET_REQUEST_CANCELLED", result.ErrorCode);
    }

    [Fact]
    public async Task RequestAsync_IgnoresResponseFromUnexpectedNode()
    {
        var channel = new TestChannel("edge") { DeliverMessages = false };
        using var endpoint = new ONETRequestResponseEndpoint(channel);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var requestTask = endpoint.RequestAsync("host", "sync", "{}", cancellation.Token);
        var sent = await channel.NextSent.Task.WaitAsync(TimeSpan.FromSeconds(1));

        channel.Receive(new ONETApplicationMessage
        {
            SourceNodeId = "attacker",
            TargetNodeId = "edge",
            Envelope = new ONETRequestResponseEnvelope
            {
                CorrelationId = sent.Envelope.CorrelationId,
                Kind = ONETRequestResponseMessageKind.Response,
                Operation = "sync",
                PayloadJson = "forged"
            }
        });

        var result = await requestTask;
        Assert.True(result.IsError);
        Assert.Equal("ONET_REQUEST_CANCELLED", result.ErrorCode);
    }

    [Fact]
    public async Task RequestAsync_PropagatesRemoteStructuredError()
    {
        var clientChannel = new TestChannel("edge");
        var hostChannel = new TestChannel("host");
        clientChannel.Peer = hostChannel;
        hostChannel.Peer = clientChannel;
        using var client = new ONETRequestResponseEndpoint(clientChannel);
        using var host = new ONETRequestResponseEndpoint(hostChannel);
        host.RegisterHandler("sync", (_, _) => Task.FromResult(Failure("SYNC_REJECTED", "bad checkpoint")));

        var result = await client.RequestAsync("host", "sync", "{}", CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("SYNC_REJECTED", result.ErrorCode);
        Assert.Equal("bad checkpoint", result.Message);
    }

    [Fact]
    public async Task RequestAsync_SendFailure_IsReturnedWithoutWaiting()
    {
        var channel = new TestChannel("edge") { SendFailure = true };
        using var endpoint = new ONETRequestResponseEndpoint(channel);

        var result = await endpoint.RequestAsync("host", "sync", "{}", CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("ONET_REQUEST_SEND_FAILED", result.ErrorCode);
    }

    [Fact]
    public async Task HyperDriveTransport_UsesAuthenticatedNodeAvatarAndReturnsHostedExchange()
    {
        var avatarId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var clientChannel = new TestChannel("edge");
        var hostChannel = new TestChannel("host");
        clientChannel.Peer = hostChannel;
        hostChannel.Peer = clientChannel;
        using var clientEndpoint = new ONETRequestResponseEndpoint(clientChannel);
        using var hostEndpoint = new ONETRequestResponseEndpoint(hostChannel);
        _ = new ONETHyperDriveSyncHost(hostEndpoint, new HostedHyperDriveSyncProcessor(new EmptyHostedStore()),
            new FixedAuthorizationResolver("edge", avatarId));
        var transport = new ONETHyperDriveSyncTransport(clientEndpoint, "host");

        var result = await transport.ExchangeAsync(new SyncExchangeRequest
        {
            AvatarId = avatarId,
            DeviceId = deviceId,
            Operations = Array.Empty<SyncOperation>(),
            MaximumRemoteChanges = 10
        }, CancellationToken.None);

        Assert.False(result.IsError, result.Message);
        Assert.NotNull(result.Result);
        Assert.Equal("checkpoint-1", result.Result.NextPullCheckpoint);
        Assert.Equal("oasis.hyperdrive.sync.exchange.v3",
            (await clientChannel.NextSent.Task).Envelope.Operation);
    }

    [Fact]
    public async Task HyperDriveHost_RejectsPayloadAvatarDifferentFromAuthenticatedPeer()
    {
        var clientChannel = new TestChannel("edge");
        var hostChannel = new TestChannel("host");
        clientChannel.Peer = hostChannel;
        hostChannel.Peer = clientChannel;
        using var clientEndpoint = new ONETRequestResponseEndpoint(clientChannel);
        using var hostEndpoint = new ONETRequestResponseEndpoint(hostChannel);
        _ = new ONETHyperDriveSyncHost(hostEndpoint, new HostedHyperDriveSyncProcessor(new EmptyHostedStore()),
            new FixedAuthorizationResolver("edge", Guid.NewGuid()));
        var transport = new ONETHyperDriveSyncTransport(clientEndpoint, "host");

        var result = await transport.ExchangeAsync(new SyncExchangeRequest
        {
            AvatarId = Guid.NewGuid(), DeviceId = Guid.NewGuid(), Operations = Array.Empty<SyncOperation>()
        }, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("HOSTED_SYNC_AVATAR_SCOPE_MISMATCH", result.ErrorCode);
    }

    private static OASISResult<string> Success(string value) => new() { Result = value };

    private static OASISResult<string> Failure(string code, string message) => new()
    {
        IsError = true, ErrorCount = 1, ErrorCode = code, Message = message
    };

    private sealed class TestChannel : IONETApplicationMessageChannel
    {
        public TestChannel(string localNodeId) => LocalNodeId = localNodeId;
        public string LocalNodeId { get; }
        public TestChannel? Peer { get; set; }
        public bool DeliverMessages { get; set; } = true;
        public bool SendFailure { get; set; }
        public TaskCompletionSource<ONETApplicationMessage> NextSent { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public event EventHandler<ONETApplicationMessage>? MessageReceived;

        public Task<OASISResult<bool>> SendAsync(ONETApplicationMessage message, CancellationToken cancellationToken)
        {
            NextSent.TrySetResult(message);
            if (SendFailure)
                return Task.FromResult(new OASISResult<bool> { IsError = true, ErrorCount = 1, Message = "offline" });
            if (DeliverMessages && Peer != null) Peer.Receive(message);
            return Task.FromResult(new OASISResult<bool> { Result = true });
        }

        public void Receive(ONETApplicationMessage message) => MessageReceived?.Invoke(this, message);
    }

    private sealed class FixedAuthorizationResolver : IONETAvatarAuthorizationResolver
    {
        private readonly string _nodeId;
        private readonly Guid _avatarId;
        public FixedAuthorizationResolver(string nodeId, Guid avatarId) { _nodeId = nodeId; _avatarId = avatarId; }
        public Task<OASISResult<Guid>> ResolveAvatarIdAsync(string authenticatedSourceNodeId, CancellationToken cancellationToken) =>
            Task.FromResult(authenticatedSourceNodeId == _nodeId
                ? new OASISResult<Guid> { Result = _avatarId }
                : new OASISResult<Guid> { IsError = true, ErrorCount = 1, ErrorCode = "AUTH_FAILED", Message = "unknown peer" });
    }

    private sealed class EmptyHostedStore : IHostedHyperDriveSyncStore
    {
        public Task<OASISResult<HostedSyncOperationBatchResult>> ApplyOperationsAsync(Guid authenticatedAvatarId,
            Guid deviceId, IReadOnlyList<SyncOperation> operations, CancellationToken cancellationToken) =>
            Task.FromResult(new OASISResult<HostedSyncOperationBatchResult>
            {
                Result = new HostedSyncOperationBatchResult { OperationResults = Array.Empty<SyncOperationResult>() }
            });

        public Task<OASISResult<HostedSyncChangeBatch>> ReadChangesAsync(Guid authenticatedAvatarId,
            Guid deviceId, string checkpoint, Guid snapshotId, int snapshotPageIndex,
            int maximumCount, CancellationToken cancellationToken) =>
            Task.FromResult(new OASISResult<HostedSyncChangeBatch>
            {
                Result = new HostedSyncChangeBatch
                {
                    Changes = Array.Empty<SyncRemoteChange>(), NextCheckpoint = "checkpoint-1"
                }
            });
    }
}
