using System.Text;
using System.Text.Json;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.Messaging;
using NextGenSoftware.OASIS.Edge.NearbySync;
using Xunit;

namespace NextGenSoftware.OASIS.Edge.Messaging.UnitTests;

internal sealed class FakeQueue : IEdgeMessageCommandQueue
{
    public readonly List<(Guid EntityId, HyperDriveMessageCommand Command)> Queued = new();
    public bool Fail;
    public Task<OASISResult<bool>> QueueAsync(Guid entityId, HyperDriveMessageCommand command, CancellationToken ct)
    {
        if (Fail) return Task.FromResult(new OASISResult<bool> { IsError = true, ErrorCode = "OUTBOX_FULL", Message = "full" });
        Queued.Add((entityId, command));
        return Task.FromResult(new OASISResult<bool> { Result = true });
    }
}

internal sealed class FakeApi : IEdgeMessagingApi
{
    public readonly List<EdgeServerSyncPage> Pages = new();
    public readonly List<DateTime?> Cursors = new();
    private int _next;
    public Task<OASISResult<EdgeServerSyncPage>> SyncAsync(DateTime? since, int limit, CancellationToken ct)
    {
        Cursors.Add(since);
        var page = _next < Pages.Count ? Pages[_next++] : new EdgeServerSyncPage { NextCursor = since };
        return Task.FromResult(new OASISResult<EdgeServerSyncPage> { Result = page });
    }
}

public class EdgeMessagingTests
{
    private static readonly Guid Alice = Guid.NewGuid();
    private static readonly Guid Bob = Guid.NewGuid();

    [Fact]
    public void ConversationId_MatchesTheServerAlgorithm()
    {
        var a = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var b = Guid.Parse("00000000-0000-0000-0000-000000000002");
        EdgeConversationId.Direct(a, b).Should().Be(Guid.Parse("3ed2b6b5-cd81-0191-5888-aa97c075dbe4"));
        EdgeConversationId.Direct(b, a).Should().Be(EdgeConversationId.Direct(a, b));
    }

    [Fact]
    public async Task Send_QueuesOneIdempotentCommand_KeyedByMessageId()
    {
        var queue = new FakeQueue();
        using var service = new EdgeMessagingService(Alice, new EdgeMessageStore(), queue);

        var sent = await service.SendAsync(Bob, "hello");

        sent.IsError.Should().BeFalse();
        sent.Result.State.Should().Be(EdgeMessageState.Queued);
        queue.Queued.Should().ContainSingle();
        queue.Queued[0].EntityId.Should().Be(sent.Result.Id);
        queue.Queued[0].Command.Action.Should().Be(HyperDriveMessageAction.Send);
        queue.Queued[0].Command.ToAvatarId.Should().Be(Bob);
        service.GetMessages(Bob).Should().ContainSingle();
    }

    [Fact]
    public async Task Send_WhenOutboxRefuses_MarksFailedAndReportsTheError()
    {
        var store = new EdgeMessageStore();
        using var service = new EdgeMessagingService(Alice, store, new FakeQueue { Fail = true });

        var sent = await service.SendAsync(Bob, "hello");

        sent.IsError.Should().BeTrue();
        sent.ErrorCode.Should().Be("OUTBOX_FULL");
        store.GetConversations(Alice).Single().LastMessage.State.Should().Be(EdgeMessageState.Failed);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Send_RejectsEmptyContent(string content)
    {
        using var service = new EdgeMessagingService(Alice, new EdgeMessageStore(), new FakeQueue());
        (await service.SendAsync(Bob, content)).IsError.Should().BeTrue();
    }

    private sealed class Pair : IDisposable
    {
        public NearbyNode AliceNode, BobNode, MalloryNode;
        public NearbyIdentity AliceId = new(), BobId = new(), MalloryId = new();
        public EdgeMessagingService AliceSvc, BobSvc;
        public EdgeMessageStore AliceStore = new(), BobStore = new();
        public FakeQueue AliceQueue = new(), BobQueue = new();

        public Pair()
        {
            var net = new LoopbackNearbyNetwork();
            var a = net.CreateLink("a"); var b = net.CreateLink("b"); var m = net.CreateLink("m");
            net.Connect(a, b); net.Connect(m, b);
            var options = new NearbyNodeOptions { DefaultRequestTimeout = TimeSpan.FromSeconds(3) };
            AliceId.TrustPeer(BobId.PublicKey, Bob); BobId.TrustPeer(AliceId.PublicKey, Alice);
            BobId.TrustPeer(MalloryId.PublicKey, Guid.NewGuid()); MalloryId.TrustPeer(BobId.PublicKey, Bob);
            AliceNode = new NearbyNode(AliceId, new[] { a }, options);
            BobNode = new NearbyNode(BobId, new[] { b }, options);
            MalloryNode = new NearbyNode(MalloryId, new[] { m }, options);
            AliceSvc = new EdgeMessagingService(Alice, AliceStore, AliceQueue, AliceNode, AliceId);
            BobSvc = new EdgeMessagingService(Bob, BobStore, BobQueue, BobNode, BobId);
        }

        public void Dispose() { AliceNode.Dispose(); BobNode.Dispose(); MalloryNode.Dispose(); AliceId.Dispose(); BobId.Dispose(); MalloryId.Dispose(); }
    }

    [Fact]
    public async Task NearbySend_DeliversDirectly_AndStillQueuesTheDurableCommand()
    {
        using var p = new Pair();
        EdgeMessage received = null;
        p.BobSvc.MessageReceived += m => received = m;

        var sent = await p.AliceSvc.SendAsync(Bob, "over bluetooth");

        sent.Result.State.Should().Be(EdgeMessageState.Delivered);
        received.Should().NotBeNull();
        received.Content.Should().Be("over bluetooth");
        received.State.Should().Be(EdgeMessageState.Received);
        p.BobSvc.GetMessages(Alice).Should().ContainSingle();
        p.AliceQueue.Queued.Should().ContainSingle("the outbox remains the durable route");
    }

    [Fact]
    public async Task Nearby_RejectsAMessageClaimingToBeFromSomeoneElse()
    {
        using var p = new Pair();
        var forged = new NearbyMessagePacket { Message = new EdgeMessage
        {
            Id = Guid.NewGuid(), FromAvatarId = Alice, ToAvatarId = Bob, Content = "I am Alice", ClientTimestamp = DateTime.UtcNow
        } };

        var reply = await p.MalloryNode.RequestAsync(p.BobNode.NodeId, NearbyChannels.Messaging,
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(forged)), null, CancellationToken.None);

        JsonSerializer.Deserialize<NearbyMessageAck>(Encoding.UTF8.GetString(reply)).Accepted.Should().BeFalse();
        p.BobSvc.GetConversations().Should().BeEmpty();
    }

    [Fact]
    public async Task ServerSync_MergesWithNearbyCopy_WithoutDuplicates_AndQueuesOneReceipt()
    {
        using var p = new Pair();
        var sent = await p.AliceSvc.SendAsync(Bob, "same message, two routes");
        var cursor = DateTime.UtcNow;
        var api = new FakeApi();
        api.Pages.Add(new EdgeServerSyncPage
        {
            NextCursor = cursor,
            Messages = { new EdgeServerMessage { Id = sent.Result.Id, FromAvatarId = Alice, ToAvatarId = Bob, Content = "same message, two routes", ReceivedAt = cursor, Timestamp = cursor, DeliveryState = 0 } }
        });

        var first = await p.BobSvc.SyncFromServerAsync(api);
        var second = await p.BobSvc.SyncFromServerAsync(api);

        first.Result.Should().Be(0, "the message already arrived nearby");
        p.BobSvc.GetMessages(Alice).Should().ContainSingle();
        p.BobQueue.Queued.Count(q => q.Command.Action == HyperDriveMessageAction.MarkDelivered).Should().Be(1);
        p.BobStore.ServerCursor.Should().Be(cursor);
        api.Cursors.Should().Contain(cursor, "the second pull resumes from the stored cursor");
        second.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task ServerSync_FollowsPaging_UntilTheServerHasNoMore()
    {
        var store = new EdgeMessageStore();
        using var service = new EdgeMessagingService(Bob, store, new FakeQueue());
        var t1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var api = new FakeApi();
        api.Pages.Add(new EdgeServerSyncPage { HasMore = true, NextCursor = t1, Messages = { Msg(Alice, Bob, "one", t1) } });
        api.Pages.Add(new EdgeServerSyncPage { HasMore = false, NextCursor = t1.AddMinutes(1), Messages = { Msg(Alice, Bob, "two", t1.AddMinutes(1)) } });

        var result = await service.SyncFromServerAsync(api);

        result.Result.Should().Be(2);
        service.GetMessages(Alice).Select(m => m.Content).Should().Equal("one", "two");
        store.ServerCursor.Should().Be(t1.AddMinutes(1));
    }

    private static EdgeServerMessage Msg(Guid from, Guid to, string content, DateTime at) => new()
    { Id = Guid.NewGuid(), FromAvatarId = from, ToAvatarId = to, Content = content, ReceivedAt = at, Timestamp = at };

    [Fact]
    public void Store_PersistsAcrossRestarts_AndNeverRegressesState()
    {
        var path = Path.Combine(Path.GetTempPath(), "edge-msg-" + Guid.NewGuid() + ".json");
        try
        {
            var id = Guid.NewGuid();
            var store = new EdgeMessageStore(path);
            store.Upsert(new EdgeMessage { Id = id, FromAvatarId = Alice, ToAvatarId = Bob, Content = "x", State = EdgeMessageState.Read });
            store.Upsert(new EdgeMessage { Id = id, Content = "tampered", State = EdgeMessageState.Queued });
            store.SetServerCursor(new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc));

            var reopened = new EdgeMessageStore(path);
            reopened.Get(id).Content.Should().Be("x", "stored content is immutable");
            reopened.Get(id).State.Should().Be(EdgeMessageState.Read);
            reopened.ServerCursor.Should().NotBeNull();
        }
        finally { File.Delete(path); File.Delete(path + ".tmp"); }
    }

    [Fact]
    public void Store_RefusesACorruptFile_InsteadOfSilentlyStartingEmpty()
    {
        var path = Path.Combine(Path.GetTempPath(), "edge-msg-" + Guid.NewGuid() + ".json");
        try
        {
            File.WriteAllText(path, "{ not json");
            Action open = () => new EdgeMessageStore(path);
            open.Should().Throw<JsonException>();
        }
        finally { File.Delete(path); }
    }
}
