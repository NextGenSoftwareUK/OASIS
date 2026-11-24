using System.Text.Json;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.Messaging;
using NextGenSoftware.OASIS.Edge.NearbySync;
using NextGenSoftware.OASIS.Edge.Runtime;
using Xunit;

namespace NextGenSoftware.OASIS.Edge.Messaging.UnitTests;

/// <summary>
/// The full Our World / HerzApp offline path with the real Edge runtime and its real SQLite outbox:
/// a message written with no internet is carried by the HyperDrive outbox over a nearby link to a gateway,
/// which applies it to the "server" exactly once even when the same send is replayed.
/// </summary>
public sealed class EdgeRuntimeNearbyEndToEndTests : IDisposable
{
    private readonly string _db = Path.Combine(Path.GetTempPath(), $"edge-msg-e2e-{Guid.NewGuid():N}.db");
    private readonly Guid _alice = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();

    public void Dispose()
    {
        foreach (var f in new[] { _db, _db + "-wal", _db + "-shm" })
            try { File.Delete(f); } catch (IOException) { }
    }

    /// <summary>Stands in for the hosted node behind a gateway: accepts every operation and applies message commands idempotently.</summary>
    private sealed class FakeHostedNode : INearbyExchangeHandler
    {
        public readonly Dictionary<Guid, (HyperDriveMessageCommand Command, Guid From)> Messages = new();
        public int ExchangeCount;

        public Task<OASISResult<SyncExchangeResponse>> HandleAsync(SyncExchangeRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref ExchangeCount);
            foreach (var op in request.Operations.Where(o => o.EntityType == HyperDriveEntityTypes.MessageCommand))
            {
                var command = JsonSerializer.Deserialize<HyperDriveMessageCommand>(op.PayloadJson!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                if (command.Action == HyperDriveMessageAction.Send)
                    Messages.TryAdd(op.EntityId, (command, request.AvatarId));
            }
            // The hosted node reports each executed command's outcome back as a CommandResult change.
            var outcomes = request.Operations.Where(o => o.EntityType == HyperDriveEntityTypes.MessageCommand).Select(op =>
            {
                var outcome = new HyperDriveCommandOutcome
                { OperationId = op.OperationId, Succeeded = true, Code = "COMMAND_COMPLETED", Message = "done", CompletedUtc = DateTime.UtcNow };
                return new SyncRemoteChange
                {
                    ChangeId = "command:" + op.OperationId.ToString("D"), EntityId = op.OperationId,
                    EntityType = HyperDriveEntityTypes.CommandResult, Kind = SyncOperationKind.Upsert,
                    VersionId = Guid.NewGuid(), PayloadJson = HyperDriveJson.Serialize(outcome), ChangedUtc = outcome.CompletedUtc
                };
            }).ToArray();
            return Task.FromResult(new OASISResult<SyncExchangeResponse>
            {
                Result = new SyncExchangeResponse
                {
                    RemoteChanges = outcomes,
                    OperationResults = request.Operations.Select(x => new SyncOperationResult
                    { OperationId = x.OperationId, Disposition = SyncOperationDisposition.Accepted, ResultVersionId = x.VersionId }).ToArray(),
                    NextPullCheckpoint = "1"
                }
            });
        }
    }

    [Fact]
    public async Task OfflineMessage_TravelsThroughTheOutboxToAGatewayExactlyOnce()
    {
        var net = new LoopbackNearbyNetwork();
        var deviceLink = net.CreateLink("alice-phone", 185);
        var gatewayLink = net.CreateLink("gateway", 185);
        net.Connect(deviceLink, gatewayLink);
        using var aliceKey = new NearbyIdentity();
        using var gatewayKey = new NearbyIdentity();
        aliceKey.TrustPeer(gatewayKey.PublicKey, Guid.NewGuid());
        gatewayKey.TrustPeer(aliceKey.PublicKey, _alice);
        var options = new NearbyNodeOptions { DefaultRequestTimeout = TimeSpan.FromSeconds(5) };
        using var aliceNode = new NearbyNode(aliceKey, new[] { deviceLink }, options);
        using var gatewayNode = new NearbyNode(gatewayKey, new[] { gatewayLink }, options);
        var hosted = new FakeHostedNode();
        _ = new NearbyPeerResponder(gatewayNode, hosted);

        var nearby = new NearbyPeerSyncTransport(aliceNode, gatewayNode.NodeId);
        using var runtime = new OASISEdgeRuntime(new EdgeRuntimeOptions
        {
            DeviceId = Guid.NewGuid(), AvatarId = _alice, DatabasePath = _db,
            CommandOutcomePollInterval = TimeSpan.FromMilliseconds(20)
        }, nearby);
        using var service = new EdgeMessagingService(_alice, new EdgeMessageStore(), new EdgeRuntimeMessageCommandQueue(runtime));

        var sent = await service.SendAsync(_bob, "written with no internet");

        sent.IsError.Should().BeFalse(sent.Message);
        sent.Result.State.Should().Be(EdgeMessageState.Queued);
        runtime.Status.PendingOperationCount.Should().Be(1, "the message is durably in the SQLite outbox");
        hosted.Messages.Should().BeEmpty("no route to the server has been used yet");

        var sync = await runtime.SetConnectivityAsync(true);
        sync.IsError.Should().BeFalse(sync.Message);

        hosted.Messages.Should().ContainSingle();
        var (command, from) = hosted.Messages[sent.Result.Id];
        command.Content.Should().Be("written with no internet");
        command.ToAvatarId.Should().Be(_bob);
        from.Should().Be(_alice, "the gateway acts for the avatar bound to the verified key");
        runtime.Status.PendingOperationCount.Should().Be(0, "the accepted operation left the outbox");
        deviceLink.FramesSent.Should().BeGreaterThan(1, "the exchange was chunked into BLE-sized frames");

        await runtime.SynchronizeAsync();
        hosted.Messages.Should().ContainSingle("re-syncing never duplicates");
    }

    [Fact]
    public async Task WhenNoNearbyPeerIsInRange_TheMessageStaysQueuedAndTheRuntimeReportsIt()
    {
        var net = new LoopbackNearbyNetwork();
        using var aliceKey = new NearbyIdentity();
        using var aliceNode = new NearbyNode(aliceKey, new[] { net.CreateLink("alone", 185) }, new NearbyNodeOptions { DefaultRequestTimeout = TimeSpan.FromMilliseconds(300) });
        using var runtime = new OASISEdgeRuntime(new EdgeRuntimeOptions { DeviceId = Guid.NewGuid(), AvatarId = _alice, DatabasePath = _db },
            new NearbyPeerSyncTransport(aliceNode, "no-such-gateway"));
        using var service = new EdgeMessagingService(_alice, new EdgeMessageStore(), new EdgeRuntimeMessageCommandQueue(runtime));

        var sent = await service.SendAsync(_bob, "still here");
        await runtime.SetConnectivityAsync(true);

        sent.IsError.Should().BeFalse();
        runtime.Status.PendingOperationCount.Should().Be(1, "nothing was lost; it waits for any route");
        runtime.Status.LastErrorCode.Should().NotBeNullOrEmpty();
    }
}
