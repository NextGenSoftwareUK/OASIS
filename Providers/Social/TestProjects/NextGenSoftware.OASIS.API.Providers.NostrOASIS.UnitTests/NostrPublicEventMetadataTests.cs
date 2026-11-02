using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.NostrOASIS.UnitTests;

[TestClass]
public sealed class NostrPublicEventMetadataTests
{
    private static readonly Uri Relay = new("wss://relay.damus.io/");
    private static readonly Guid Owner = Guid.Parse("e99abf18-c92f-48b7-aeb6-c5945df1abc1");
    private static string[] Events()
    {
        using var data = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData/captured-public-events.json")));
        return data.RootElement.EnumerateArray().Select(row => row.GetRawText()).ToArray();
    }
    private static string Id(string raw) => JsonNode.Parse(raw)!["id"]!.GetValue<string>();

    [DataTestMethod]
    [DataRow("all")] [DataRow("subset")] [DataRow("point")] [DataRow("page")]
    [DataRow("missing")] [DataRow("duplicate")] [DataRow("unexpected")] [DataRow("tampered")]
    [DataRow("cancel")] [DataRow("budget")] [DataRow("unknown")] [DataRow("inactive")]
    [DataRow("empty-page-tampered")]
    public async Task ControlledQueryRequiresCompleteVerifiedSelection(string scenario)
    {
        var native = Events(); var keys = native.Select(Id).ToArray(); string fault = ""; int calls = 0;
        Task<IReadOnlyList<string>> Read(IReadOnlyList<string> selected, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); calls++;
            var rows = selected.Select(key => native.Single(row => Id(row) == key)).Reverse().ToList();
            if (fault == "missing") rows.RemoveAt(rows.Count - 1);
            if (fault == "duplicate") rows[rows.Count - 1] = rows[0];
            if (fault == "unexpected") rows[0] = native[2];
            if (fault is "tampered" or "empty-page-tampered")
            {
                var value = JsonNode.Parse(rows[0])!; value["content"] = "tampered"; rows[0] = value.ToJsonString();
            }
            return Task.FromResult<IReadOnlyList<string>>(rows);
        }
        using var source = new NostrPublicEventMetadataSource(Relay, keys, Owner, Read);
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        fault = scenario; calls = 0;
        var selected = scenario == "unknown" ? new[] { new string('0', 64) }
            : scenario is "subset" or "unexpected" ? keys.Take(2).ToArray() : keys;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.NostrOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, NostrPublicEventMetadataSource.AddressMetadata))).Result;
        if (scenario == "point")
        {
            string key = keys[0];
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.NostrOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, NostrPublicEventMetadataSource.AddressMetadata) == key).Result;
        }
        if (scenario == "page") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-page-tampered") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.NostrOASIS,
            new[] { new CosmicQueryTarget(ProviderType.NostrOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancellation = new CancellationTokenSource();
        if (scenario == "cancel") cancellation.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancellation.Token);
        if (scenario is "all" or "subset" or "point" or "page")
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(scenario is "point" or "page" ? 1 : scenario == "subset" ? 2 : 3, result.Result.Count);
            Assert.AreEqual(scenario == "page" ? keys[1] : keys[0], result.Result[0].MetaData["NostrEventId"]);
            foreach (var row in result.Result)
            {
                var original = NostrPublicEventVerification.Parse(native.Single(text => Id(text) == row.MetaData["NostrEventId"].ToString()));
                Assert.AreEqual(original.Content, row.Description);
                Assert.AreEqual(original.Signature, row.MetaData["NostrSignature"]);
                Assert.AreEqual(original.PublicKey, row.MetaData["NostrPublicKey"]);
                Assert.AreEqual("signed-event-relay-observed", row.MetaData["NostrObservationPolicy"]);
                Assert.AreEqual(Owner, row.CreatedByAvatarId);
                Assert.IsTrue(row.IsPublic);
            }
        }
        else
        {
            Assert.IsTrue(result.IsError, scenario);
            Assert.IsNull(result.Result);
            if (scenario is "unknown" or "cancel" or "inactive") Assert.AreEqual(0, calls);
        }
    }

    [TestMethod]
    public async Task UnsupportedTextQueryRejectsBeforeNativeReadAndSourceCannotSaveOrDelete()
    {
        int calls = 0; string[] keys = Events().Select(Id).ToArray();
        using var source = new NostrPublicEventMetadataSource(Relay, keys, Owner,
            (_, _) => { calls++; throw new InvalidOperationException("No native read expected."); });
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.NostrOASIS).Result)
            .Where(row => row.Name == "unbounded search").Result;
        Assert.IsTrue(CosmicQueryPlanner.Plan(query, ProviderType.NostrOASIS,
            new[] { new CosmicQueryTarget(ProviderType.NostrOASIS, source.QueryCapabilities) }).IsError);
        Assert.AreEqual(0, calls);
        var saved = await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), IsNewHolon = true });
        Assert.IsTrue(saved.IsError);
        Assert.IsTrue((await source.DeleteHolonAsync(Guid.NewGuid())).IsError);
        Assert.AreEqual(0, calls);
    }

    [DataTestMethod]
    [DataRow("credential")] [DataRow("duplicate")] [DataRow("owner")] [DataRow("http")]
    [DataRow("path")] [DataRow("uppercase")] [DataRow("extra")]
    public void InvalidSourceConfigurationRejectsWithoutNetwork(string scenario)
    {
        var config = new JsonObject { ["schema"] = 1, ["relayUrl"] = Relay.AbsoluteUri,
            ["sourceOwner"] = Owner.ToString("D"), ["eventIds"] = new JsonArray(Events().Select(row => (JsonNode)JsonValue.Create(Id(row))!).ToArray()) };
        switch (scenario)
        {
            case "credential": config["relayUrl"] = "wss://user:pass@relay.damus.io/"; break;
            case "duplicate": config["eventIds"]![1] = config["eventIds"]![0]!.GetValue<string>(); break;
            case "owner": config["sourceOwner"] = Guid.Empty.ToString(); break;
            case "http": config["relayUrl"] = "https://relay.damus.io/"; break;
            case "path": config["relayUrl"] = "wss://relay.damus.io/other"; break;
            case "uppercase": config["eventIds"]![0] = config["eventIds"]![0]!.GetValue<string>().ToUpperInvariant(); break;
            case "extra": config["secret"] = "forbidden-inert-value"; break;
        }
        Assert.ThrowsException<ArgumentException>(() => NostrPublicEventMetadataSource.FromJson(config.ToJsonString()));
    }

    private sealed record Frame(byte[] Bytes, bool End = true, WebSocketMessageType Type = WebSocketMessageType.Text);
    private sealed class Socket : WebSocket
    {
        public readonly Queue<Frame> Frames = new();
        public readonly List<string> Sent = new();
        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override WebSocketState State => WebSocketState.Open;
        public override string? SubProtocol => null;
        public override void Abort() { }
        public override void Dispose() { }
        public override Task CloseAsync(WebSocketCloseStatus status, string? description, CancellationToken token) => Task.CompletedTask;
        public override Task CloseOutputAsync(WebSocketCloseStatus status, string? description, CancellationToken token) => Task.CompletedTask;
        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType type, bool end, CancellationToken token)
        { token.ThrowIfCancellationRequested(); Sent.Add(Encoding.UTF8.GetString(buffer)); return Task.CompletedTask; }
        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (Frames.Count == 0) return Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));
            var frame = Frames.Dequeue(); frame.Bytes.CopyTo(buffer.AsSpan());
            return Task.FromResult(new WebSocketReceiveResult(frame.Bytes.Length, frame.Type, frame.End));
        }
        public void Message(string text) => Frames.Enqueue(new(Encoding.UTF8.GetBytes(text)));
    }

    [DataTestMethod]
    [DataRow("fragmented")] [DataRow("closed")] [DataRow("binary")] [DataRow("subscription")]
    [DataRow("notice")] [DataRow("auth")] [DataRow("utf8")] [DataRow("frames")]
    [DataRow("message-size")] [DataRow("total-size")] [DataRow("event-count")]
    public async Task SocketReadRequiresMatchedEoseWithinEveryBound(string scenario)
    {
        var raw = Events()[0]; var ids = new[] { Id(raw) }; using var socket = new Socket();
        string frame = "[\"EVENT\",\"unit\"," + raw + "]";
        if (scenario == "fragmented")
        {
            byte[] bytes = Encoding.UTF8.GetBytes(frame); int split = bytes.Length / 2;
            socket.Frames.Enqueue(new(bytes[..split], false)); socket.Frames.Enqueue(new(bytes[split..]));
            socket.Message("[\"EOSE\",\"unit\"]");
        }
        else if (scenario == "closed") socket.Message(frame);
        else if (scenario == "binary") socket.Frames.Enqueue(new(new byte[] { 1 }, true, WebSocketMessageType.Binary));
        else if (scenario == "subscription") socket.Message("[\"EOSE\",\"foreign\"]");
        else if (scenario == "notice") socket.Message("[\"NOTICE\",\"blocked\"]");
        else if (scenario == "auth") socket.Message("[\"AUTH\",\"unit\"]");
        else if (scenario == "utf8") socket.Frames.Enqueue(new(new byte[] { 0xff }));
        else if (scenario == "event-count") for (int x = 0; x < 9; x++) socket.Message(frame);
        else
        {
            int count = scenario == "frames" ? 65 : scenario == "message-size" ? 9 : 40;
            for (int x = 0; x < count; x++)
            {
                // Aggregate-size case uses separate valid JSON messages below64KiB.
                if (scenario == "total-size")
                {
                    string body = "[\"EVENT\",\"unit\",\"" + new string('x', 40000) + "\"]";
                    byte[] data = Encoding.UTF8.GetBytes(body);
                    for (int start = 0; start < data.Length; start += 8192)
                        socket.Frames.Enqueue(new(data[start..Math.Min(start + 8192, data.Length)], start + 8192 >= data.Length));
                    if (x == 6) break;
                }
                else socket.Frames.Enqueue(new(new byte[scenario == "frames" ? 0 : 8192], false));
            }
        }
        try
        {
            var result = await NostrPublicRelayRead.ReadConnectedAsync(socket, ids, "unit", default);
            Assert.AreEqual("fragmented", scenario);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(ids[0], NostrPublicEventVerification.Parse(result[0]).Id);
        }
        catch (Exception error) when (error is InvalidDataException or DecoderFallbackException)
        { Assert.AreNotEqual("fragmented", scenario, error.ToString()); }
        Assert.AreEqual(1, socket.Sent.Count);
        using var sent = JsonDocument.Parse(socket.Sent[0]);
        Assert.AreEqual("REQ", sent.RootElement[0].GetString());
        Assert.AreEqual(ids[0], sent.RootElement[2].GetProperty("ids")[0].GetString());
    }
}
