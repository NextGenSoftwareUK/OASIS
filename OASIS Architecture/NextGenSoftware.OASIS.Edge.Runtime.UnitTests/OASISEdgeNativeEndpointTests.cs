using System.Net;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Native.EndPoint.Edge;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.Runtime;
using NextGenSoftware.OASIS.ONET;
using Xunit;

namespace NextGenSoftware.OASIS.Edge.Runtime.UnitTests;

public sealed class OASISEdgeNativeEndpointTests
{
    [Fact]
    public async Task HttpCompositionSynchronizesQueuedEntityThroughHostedApi()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"oasis-edge-http-{Guid.NewGuid():N}.db");
        var avatarId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var handler = new HostedSyncHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://hosted-oasis.test/") };
        try
        {
            using var api = new OASISEdgeAPI(new EdgeRuntimeOptions
            {
                AvatarId = avatarId, DeviceId = deviceId, DatabasePath = databasePath,
                PayloadSerializer = new EdgePayloadSerializer(EdgeTestJsonContext.Default)
            }, client);
            var statusEvents = new List<EdgeSynchronizationState>();
            api.StatusChanged += (_, status) => statusEvents.Add(status.Synchronization);

            var saved = await api.SaveEntityAsync(new EdgeEntityWriteRequest<TestEntity>
            {
                OperationId = Guid.NewGuid(), EntityId = Guid.NewGuid(), EntityType = "test",
                VersionId = Guid.NewGuid(), CreatedUtc = DateTime.UtcNow,
                Entity = new TestEntity { Name = "queued" }
            });
            var synchronized = await api.SetConnectivityAsync(true);

            Assert.False(saved.IsError, saved.Message);
            Assert.False(synchronized.IsError, synchronized.Message);
            Assert.Equal(1, handler.ExchangeCount);
            Assert.Equal(avatarId, handler.LastRequest.AvatarId);
            Assert.Equal(deviceId, handler.LastRequest.DeviceId);
            Assert.Single(handler.LastRequest.Operations);
            Assert.Equal(0, api.Status.PendingOperationCount);
            Assert.Equal(EdgeSynchronizationState.Synchronized, api.Status.Synchronization);
            Assert.Contains(EdgeSynchronizationState.Synchronizing, statusEvents);
            Assert.Contains(EdgeSynchronizationState.Synchronized, statusEvents);
        }
        finally { if (File.Exists(databasePath)) File.Delete(databasePath); }
    }

    [Fact]
    public async Task EdgeOnetFacadePersistsOfflineEntityWithoutFullOnode()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"oasis-edge-native-{Guid.NewGuid():N}.db");
        try
        {
            using var api = new OASISEdgeOnetAPI(new EdgeRuntimeOptions
            {
                AvatarId = Guid.NewGuid(), DeviceId = Guid.NewGuid(), DatabasePath = databasePath,
                PayloadSerializer = new EdgePayloadSerializer(EdgeTestJsonContext.Default)
            }, new DisconnectedChannel("edge-node"), "host-node", new Binder());
            var entityId = Guid.NewGuid();

            var saved = await api.SaveEntityAsync(new EdgeEntityWriteRequest<TestEntity>
            {
                OperationId = Guid.NewGuid(), EntityId = entityId, EntityType = "test",
                VersionId = Guid.NewGuid(), CreatedUtc = DateTime.UtcNow,
                Entity = new TestEntity { Name = "offline" }
            });
            var loaded = await api.LoadEntityAsync<TestEntity>("test", entityId);

            Assert.False(saved.IsError, saved.Message);
            Assert.False(loaded.IsError, loaded.Message);
            Assert.Equal("offline", loaded.Result.Entity.Name);
            Assert.Equal(EdgeSynchronizationState.Pending, api.Status.Synchronization);
        }
        finally { if (File.Exists(databasePath)) File.Delete(databasePath); }
    }

    [Fact]
    public async Task NativeFacadeQueuesCanonicalQuestCommandAndSynchronizesIt()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"oasis-edge-command-{Guid.NewGuid():N}.db");
        var handler = new HostedSyncHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://hosted-oasis.test/") };
        try
        {
            using var api = new OASISEdgeAPI(new EdgeRuntimeOptions
            {
                AvatarId = Guid.NewGuid(), DeviceId = Guid.NewGuid(), DatabasePath = databasePath
            }, client);
            Guid operationId = Guid.NewGuid();
            Guid questId = Guid.NewGuid();
            var queued = await api.QueueQuestProgressAsync(operationId, questId,
                new HyperDriveQuestProgressCommand { GameSource = "ODOOM", MonstersKilledDelta = 1 });
            var synchronized = await api.SetConnectivityAsync(true);

            Assert.False(queued.IsError, queued.Message);
            Assert.False(synchronized.IsError, synchronized.Message);
            var sent = Assert.Single(handler.LastRequest.Operations);
            Assert.Equal(operationId, sent.OperationId);
            Assert.Equal(questId, sent.EntityId);
            Assert.Equal(HyperDriveEntityTypes.QuestProgress, sent.EntityType);
            Assert.Equal(SyncOperationKind.Command, sent.Kind);
        }
        finally { if (File.Exists(databasePath)) File.Delete(databasePath); }
    }

    internal sealed class TestEntity { public string Name { get; set; } = string.Empty; }

    private sealed class DisconnectedChannel : IONETApplicationMessageChannel
    {
        public DisconnectedChannel(string nodeId) => LocalNodeId = nodeId;
        public string LocalNodeId { get; }
        public event EventHandler<ONETApplicationMessage> MessageReceived;
        public Task<OASISResult<bool>> SendAsync(ONETApplicationMessage message, CancellationToken cancellationToken) =>
            Task.FromResult(new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = "OFFLINE" });
    }

    private sealed class Binder : IHyperDrivePeerBindingTransport
    {
        public Task<OASISResult<bool>> BindPeerAsync(BindHyperDrivePeerRequest request,
            CancellationToken cancellationToken) => Task.FromResult(new OASISResult<bool>(true));
    }

    private sealed class HostedSyncHandler : HttpMessageHandler
    {
        public int ExchangeCount { get; private set; }
        public SyncExchangeRequest LastRequest { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.Equal("/api/hyperdrive/sync/exchange", request.RequestUri.AbsolutePath);
            LastRequest = JsonSerializer.Deserialize<SyncExchangeRequest>(
                await request.Content.ReadAsStringAsync(cancellationToken),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            ExchangeCount++;
            var operation = Assert.Single(LastRequest.Operations);
            var response = new OASISResult<SyncExchangeResponse>(new SyncExchangeResponse
            {
                NextPullCheckpoint = "1",
                OperationResults = new[]
                {
                    new SyncOperationResult
                    {
                        OperationId = operation.OperationId,
                        Disposition = SyncOperationDisposition.Accepted,
                        ResultVersionId = operation.VersionId
                    }
                },
                RemoteChanges = Array.Empty<SyncRemoteChange>()
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
            };
        }
    }
}
