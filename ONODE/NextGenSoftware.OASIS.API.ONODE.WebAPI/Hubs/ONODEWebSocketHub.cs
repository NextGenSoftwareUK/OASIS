using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using WS = System.Net.WebSockets.WebSocket;
using WebSocketMessageType = System.Net.WebSockets.WebSocketMessageType;
using WebSocketState = System.Net.WebSockets.WebSocketState;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Hubs;

/// <summary>
/// Manages native WebSocket connections from OPORTAL clients.
/// When ONODEService pushes node state (PUT /api/v1/onode/node-state/{nodeId}),
/// the controller calls BroadcastAsync() to push it instantly to all connected browsers
/// watching that nodeId — eliminating the 5s polling lag in local mode.
/// </summary>
public class ONODEWebSocketHub
{
    // nodeId → set of open WebSocket connections watching that node
    private static readonly ConcurrentDictionary<string, ConcurrentBag<WS>> _connections = new();

    public static void Register(string nodeId, WS ws)
    {
        var bag = _connections.GetOrAdd(nodeId, _ => new ConcurrentBag<WS>());
        bag.Add(ws);
    }

    public static void Unregister(string nodeId, WS ws)
    {
        // Bags don't support remove; the Send loop skips closed sockets
        // and cleanup happens in BroadcastAsync.
    }

    /// <summary>Sends the serialised payload to every open socket watching nodeId.</summary>
    public static async Task BroadcastAsync(string nodeId, object payload)
    {
        if (!_connections.TryGetValue(nodeId, out var bag) || bag.IsEmpty) return;

        var json    = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var bytes   = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        var dead    = new List<WS>();
        foreach (var ws in bag)
        {
            if (ws.State == WebSocketState.Open)
            {
                try { await ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None); }
                catch { dead.Add(ws); }
            }
            else dead.Add(ws);
        }

        // Rebuild bag without dead sockets (ConcurrentBag doesn't support remove)
        if (dead.Any())
        {
            var live = new ConcurrentBag<WS>(bag.Except(dead));
            _connections[nodeId] = live;
            foreach (var ws in dead)
                try { ws.Dispose(); } catch { }
        }
    }

    public static int ConnectionCount(string nodeId) =>
        _connections.TryGetValue(nodeId, out var bag)
            ? bag.Count(ws => ws.State == WebSocketState.Open)
            : 0;
}
