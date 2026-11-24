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

namespace NextGenSoftware.OASIS.API.Providers.NostrOASIS;

/// <summary>
/// Key-value storage on Nostr relays using NIP-78 application data: each key is one parameterized replaceable event
/// (kind 30078, <c>d</c> tag = key) authored by the configured key; its content is the (already encrypted) value.
/// A write is acknowledged only when every configured relay answers OK. Reads verify every event's id and BIP-340
/// signature and keep the newest event per key. A delete replaces the event with an empty-content tombstone and also
/// publishes a NIP-09 deletion for the address.
/// </summary>
internal sealed class NostrRelayKeyValueBackend : IKeyValueBackend
{
    private const int ApplicationDataKind = 30078;
    private const int DeletionKind = 5;
    private readonly string[] _relays;
    private readonly NostrEventSigner _signer;
    private readonly SemaphoreSlim _writes = new(1, 1);
    private long _lastCreatedAt;

    public NostrRelayKeyValueBackend(string[] relays, string secretKeyHex)
    {
        if (relays == null || relays.Length == 0) throw new ArgumentException("At least one Nostr relay is required.", nameof(relays));
        _relays = relays;
        _signer = new NostrEventSigner(secretKeyHex);
    }

    public string PublicKeyHex => _signer.PublicKeyHex;

    public async Task VerifyAsync(CancellationToken cancellationToken = default) => await QueryAsync(new JsonObject
    {
        ["kinds"] = new JsonArray(ApplicationDataKind), ["authors"] = new JsonArray(_signer.PublicKeyHex), ["limit"] = 1
    }, cancellationToken);

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var latest = await LatestByKeyAsync(new JsonObject
        {
            ["kinds"] = new JsonArray(ApplicationDataKind), ["authors"] = new JsonArray(_signer.PublicKeyHex), ["#d"] = new JsonArray(key)
        }, cancellationToken);
        return latest.TryGetValue(key, out var content) && content.Length > 0 ? content : null;
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var latest = await LatestByKeyAsync(new JsonObject
        {
            ["kinds"] = new JsonArray(ApplicationDataKind), ["authors"] = new JsonArray(_signer.PublicKeyHex), ["limit"] = 100000
        }, cancellationToken);
        return latest.Where(pair => pair.Value.Length > 0 && pair.Key.StartsWith(prefix ?? string.Empty, StringComparison.Ordinal))
            .Select(pair => pair.Key).OrderBy(key => key, StringComparer.Ordinal).ToArray();
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0) throw new InvalidDataException("Nostr storage values cannot be empty (empty content is the tombstone).");
        await PublishAsync(ApplicationDataKind, new JsonArray(new JsonArray("d", key)), value, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (await GetAsync(key, cancellationToken) == null) return; // deleting a missing key succeeds
        await PublishAsync(ApplicationDataKind, new JsonArray(new JsonArray("d", key)), string.Empty, cancellationToken);
        await PublishAsync(DeletionKind, new JsonArray(new JsonArray("a", $"{ApplicationDataKind}:{_signer.PublicKeyHex}:{key}")), string.Empty, cancellationToken);
    }

    /// <summary>Replaceable events are ordered by created_at; writes from this backend use strictly increasing seconds.</summary>
    private async Task PublishAsync(int kind, JsonArray tags, string content, CancellationToken cancellationToken)
    {
        await _writes.WaitAsync(cancellationToken);
        try
        {
            var createdAt = Math.Max(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), _lastCreatedAt + 1);
            _lastCreatedAt = createdAt;
            var evt = _signer.Sign(kind, createdAt, tags, content);
            var id = evt["id"]!.GetValue<string>();
            var message = new JsonArray("EVENT", evt).ToJsonString();
            foreach (var relay in _relays)
            {
                using var socket = await ConnectAsync(relay, cancellationToken);
                await SendAsync(socket, message, cancellationToken);
                while (true)
                {
                    using var frame = JsonDocument.Parse(await ReceiveAsync(socket, cancellationToken));
                    var root = frame.RootElement;
                    if (root[0].GetString() != "OK" || root[1].GetString() != id) continue;
                    if (!root[2].GetBoolean())
                        throw new InvalidOperationException($"Nostr relay {relay} rejected the storage event: {root[3].GetString()}");
                    break;
                }
            }
        }
        finally { _writes.Release(); }
    }

    private async Task<Dictionary<string, string>> LatestByKeyAsync(JsonObject filter, CancellationToken cancellationToken)
    {
        var newest = new Dictionary<string, (long CreatedAt, string Id, string Content)>(StringComparer.Ordinal);
        foreach (var evt in await QueryAsync(filter, cancellationToken))
        {
            if (evt.GetProperty("kind").GetInt32() != ApplicationDataKind || evt.GetProperty("pubkey").GetString() != _signer.PublicKeyHex)
                continue;
            if (!NostrEventSigner.Verify(evt))
                throw new InvalidDataException("A Nostr relay returned a storage event with an invalid id or signature.");
            var key = evt.GetProperty("tags").EnumerateArray()
                .Where(tag => tag.GetArrayLength() >= 2 && tag[0].GetString() == "d").Select(tag => tag[1].GetString()).SingleOrDefault();
            if (key == null) continue;
            var createdAt = evt.GetProperty("created_at").GetInt64();
            var id = evt.GetProperty("id").GetString()!;
            // NIP-01: the newest created_at wins; ties keep the lowest id.
            if (!newest.TryGetValue(key, out var current) || createdAt > current.CreatedAt ||
                (createdAt == current.CreatedAt && string.CompareOrdinal(id, current.Id) < 0))
                newest[key] = (createdAt, id, evt.GetProperty("content").GetString()!);
        }
        return newest.ToDictionary(pair => pair.Key, pair => pair.Value.Content, StringComparer.Ordinal);
    }

    private async Task<List<JsonElement>> QueryAsync(JsonObject filter, CancellationToken cancellationToken)
    {
        var events = new List<JsonElement>();
        foreach (var relay in _relays)
        {
            var subscription = Guid.NewGuid().ToString("N");
            using var socket = await ConnectAsync(relay, cancellationToken);
            await SendAsync(socket, new JsonArray("REQ", subscription, filter.DeepClone()).ToJsonString(), cancellationToken);
            while (true)
            {
                using var frame = JsonDocument.Parse(await ReceiveAsync(socket, cancellationToken));
                var root = frame.RootElement;
                var type = root[0].GetString();
                if (type == "EVENT" && root[1].GetString() == subscription) events.Add(root[2].Clone());
                else if (type == "EOSE" && root[1].GetString() == subscription) break;
                else if (type == "CLOSED" && root[1].GetString() == subscription)
                    throw new InvalidOperationException($"Nostr relay {relay} closed the storage query: {root[2].GetString()}");
            }
            await SendAsync(socket, new JsonArray("CLOSE", subscription).ToJsonString(), cancellationToken);
        }
        return events;
    }

    private static async Task<ClientWebSocket> ConnectAsync(string relay, CancellationToken cancellationToken)
    {
        var socket = new ClientWebSocket();
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            await socket.ConnectAsync(new Uri(relay), deadline.Token);
            return socket;
        }
        catch { socket.Dispose(); throw; }
    }

    private static Task SendAsync(ClientWebSocket socket, string message, CancellationToken cancellationToken)
        => socket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, cancellationToken);

    private static async Task<string> ReceiveAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        var buffer = new byte[64 * 1024];
        using var message = new MemoryStream();
        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, deadline.Token);
            if (result.MessageType == WebSocketMessageType.Close) throw new IOException("The Nostr relay closed the connection.");
            message.Write(buffer, 0, result.Count);
            if (message.Length > 16 * 1024 * 1024) throw new InvalidDataException("A Nostr relay frame exceeds 16 MiB.");
            if (result.EndOfMessage) return Encoding.UTF8.GetString(message.ToArray());
        }
    }
}
