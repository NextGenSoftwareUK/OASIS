using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GUNOASIS;

/// <summary>
/// Key-value storage on a GUN relay through the GUN wire protocol. Messages are DAM JSON frames:
/// put <c>{"#":id,"put":{soul:{"_":{"#":soul,"&gt;":{field:state}},field:value}}}</c> acknowledged by
/// <c>{"@":id,"ok":...}</c> or <c>{"@":id,"err":...}</c>; get <c>{"#":id,"get":{"#":soul}}</c> answered by
/// <c>{"@":id,"put":{soul:node}}</c>, without <c>put</c> when the node does not exist.
/// Conflicts resolve by GUN's HAM state per field; this backend writes strictly increasing millisecond states.
/// </summary>
internal sealed class GunRelayKeyValueBackend : IKeyValueBackend
{
    private const string ValueField = "v";
    private const string RecordSoulPrefix = "oasis.kv/";
    private const string IndexSoulPrefix = "oasis.kv.index/";
    private static readonly TimeSpan ReplyTimeout = TimeSpan.FromSeconds(30);
    private readonly Uri _socket;
    private readonly SemaphoreSlim _writes = new(1, 1);
    private long _lastState;

    public GunRelayKeyValueBackend(Uri socket) => _socket = socket ?? throw new ArgumentNullException(nameof(socket));

    /// <summary>Maps a relay base URL (http/https/ws/wss) to its <c>/gun</c> WebSocket endpoint.</summary>
    public static Uri SocketUri(string relayUrl)
    {
        if (string.IsNullOrWhiteSpace(relayUrl)) throw new ArgumentException("A GUN relay URL is required.", nameof(relayUrl));
        var builder = new UriBuilder(relayUrl.TrimEnd('/'));
        builder.Scheme = builder.Scheme switch
        {
            "http" or "ws" => "ws",
            "https" or "wss" => "wss",
            _ => throw new ArgumentException("A GUN relay URL must use http, https, ws or wss.", nameof(relayUrl))
        };
        if (!builder.Path.EndsWith("/gun", StringComparison.Ordinal)) builder.Path = builder.Path.TrimEnd('/') + "/gun";
        return builder.Uri;
    }

    private static string Directory(string key) => key[..(key.LastIndexOf('/') + 1)];

    public async Task VerifyAsync(CancellationToken cancellationToken = default) =>
        await ReadNodeAsync(IndexSoulPrefix + "__oasis_verify__/", cancellationToken);

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var node = await ReadNodeAsync(RecordSoulPrefix + key, cancellationToken);
        if (node == null || !node.TryGetPropertyValue(ValueField, out var value) || value == null) return null;
        return value.GetValueKind() == JsonValueKind.String
            ? value.GetValue<string>()
            : throw new InvalidDataException($"GUN node '{RecordSoulPrefix + key}' holds a non-string record value.");
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        if (prefix == null || !prefix.EndsWith("/", StringComparison.Ordinal))
            throw new NotSupportedException("GUN key listing is by directory; prefixes must end with '/'.");
        var node = await ReadNodeAsync(IndexSoulPrefix + prefix, cancellationToken);
        if (node == null) return Array.Empty<string>();
        return node.Where(field => field.Key != "_" && field.Value != null && field.Key.StartsWith(prefix, StringComparison.Ordinal))
            .Select(field => field.Key).OrderBy(key => key, StringComparer.Ordinal).ToArray();
    }

    public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        return WriteAsync(key, JsonValue.Create(value), JsonValue.Create(1), cancellationToken);
    }

    // Writing null is GUN's tombstone; deleting a key that never existed is also just a null write and succeeds.
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        return WriteAsync(key, null, null, cancellationToken);
    }

    /// <summary>Writes the record node and its index entry in one put message, acknowledged together.</summary>
    private async Task WriteAsync(string key, JsonNode recordValue, JsonNode indexValue, CancellationToken cancellationToken)
    {
        await _writes.WaitAsync(cancellationToken);
        try
        {
            var state = Math.Max(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), _lastState + 1);
            _lastState = state;
            var recordSoul = RecordSoulPrefix + key;
            var indexSoul = IndexSoulPrefix + Directory(key);
            var graph = new JsonObject
            {
                [recordSoul] = Node(recordSoul, ValueField, recordValue, state),
                [indexSoul] = Node(indexSoul, key, indexValue, state)
            };
            var id = NewMessageId();
            var reply = await RoundTripAsync(new JsonObject { ["#"] = id, ["put"] = graph }, id, cancellationToken);
            if (!reply.TryGetProperty("ok", out var ok) || ok.ValueKind is JsonValueKind.False or JsonValueKind.Null)
                throw new IOException($"The GUN relay did not acknowledge the write of '{key}'.");
        }
        finally { _writes.Release(); }
    }

    private static JsonObject Node(string soul, string field, JsonNode value, long state) => new()
    {
        ["_"] = new JsonObject { ["#"] = soul, [">"] = new JsonObject { [field] = state } },
        [field] = value
    };

    /// <summary>
    /// Reads one node. A relay streams a large node as several replies to the same request; every reply except the last
    /// carries <c>%</c> (more to come). Fields are merged by their HAM state so a later chunk never loses a newer value.
    /// </summary>
    private async Task<JsonObject> ReadNodeAsync(string soul, CancellationToken cancellationToken)
    {
        var id = NewMessageId();
        JsonObject merged = null;
        var states = new Dictionary<string, double>(StringComparer.Ordinal);
        await ExchangeAsync(new JsonObject { ["#"] = id, ["get"] = new JsonObject { ["#"] = soul } }, id, reply =>
        {
            if (reply.TryGetProperty("put", out var put) && put.ValueKind == JsonValueKind.Object && put.TryGetProperty(soul, out var node))
            {
                if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty("_", out var meta) || !meta.TryGetProperty(">", out var stateOf))
                    throw new InvalidDataException($"The GUN relay returned a malformed node for '{soul}'.");
                merged ??= new JsonObject();
                foreach (var field in node.EnumerateObject())
                {
                    if (field.Name == "_") continue;
                    var state = stateOf.TryGetProperty(field.Name, out var s) && s.ValueKind == JsonValueKind.Number ? s.GetDouble()
                        : throw new InvalidDataException($"The GUN relay returned field '{field.Name}' of '{soul}' without a state.");
                    if (states.TryGetValue(field.Name, out var known) && known >= state) continue;
                    states[field.Name] = state;
                    merged[field.Name] = JsonNode.Parse(field.Value.GetRawText());
                }
            }
            return !reply.TryGetProperty("%", out _);
        }, cancellationToken);
        return merged;
    }

    /// <summary>Sends one DAM message and returns the relay's single reply to it (matched by <c>@</c>).</summary>
    private async Task<JsonElement> RoundTripAsync(JsonObject message, string id, CancellationToken cancellationToken)
    {
        JsonElement result = default;
        await ExchangeAsync(message, id, reply => { result = reply.Clone(); return true; }, cancellationToken);
        return result;
    }

    /// <summary>Sends one DAM message and hands each reply to it to <paramref name="onReply"/> until that returns true.
    /// A reply carrying <c>err</c> throws.</summary>
    private async Task ExchangeAsync(JsonObject message, string id, Func<JsonElement, bool> onReply, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(ReplyTimeout);
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(_socket, deadline.Token);
        await socket.SendAsync(Encoding.UTF8.GetBytes(message.ToJsonString()), WebSocketMessageType.Text, true, deadline.Token);
        while (true)
        {
            using var frame = JsonDocument.Parse(await ReceiveAsync(socket, deadline.Token));
            foreach (var reply in Messages(frame.RootElement))
            {
                if (!reply.TryGetProperty("@", out var to) || to.ValueKind != JsonValueKind.String || to.GetString() != id) continue;
                if (reply.TryGetProperty("err", out var err) && err.ValueKind is not (JsonValueKind.Null or JsonValueKind.False))
                    throw new IOException($"The GUN relay rejected the request: {err}");
                if (!onReply(reply)) continue;
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                return;
            }
        }
    }

    // A relay frame is one DAM message or a batch (JSON array) of them.
    private static IEnumerable<JsonElement> Messages(JsonElement root) =>
        root.ValueKind == JsonValueKind.Array ? root.EnumerateArray().Where(m => m.ValueKind == JsonValueKind.Object)
        : root.ValueKind == JsonValueKind.Object ? new[] { root } : Array.Empty<JsonElement>();

    private static string NewMessageId() => Guid.NewGuid().ToString("N");

    private static async Task<string> ReceiveAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        using var message = new MemoryStream();
        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close) throw new IOException("The GUN relay closed the connection.");
            message.Write(buffer, 0, result.Count);
            if (message.Length > 16 * 1024 * 1024) throw new InvalidDataException("A GUN relay frame exceeds 16 MiB.");
            if (result.EndOfMessage) return Encoding.UTF8.GetString(message.ToArray());
        }
    }
}
