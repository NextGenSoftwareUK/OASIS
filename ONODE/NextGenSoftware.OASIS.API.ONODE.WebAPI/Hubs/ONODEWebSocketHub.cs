using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using WS = System.Net.WebSockets.WebSocket;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Hubs;

/// <summary>Owns authenticated ONODE live-state subscriptions until their request closes.</summary>
public class ONODEWebSocketHub
{
    private static readonly object RegistryGate = new();
    private static readonly Dictionary<string, Dictionary<WS, SemaphoreSlim>> Connections = new(StringComparer.Ordinal);

    public static void Register(string nodeId, WS socket)
    {
        lock (RegistryGate)
        {
            if (!Connections.TryGetValue(nodeId, out var subscribers))
                Connections[nodeId] = subscribers = new();
            if (!subscribers.ContainsKey(socket))
                subscribers.Add(socket, new SemaphoreSlim(1, 1));
        }
    }

    public static void Unregister(string nodeId, WS socket)
    {
        lock (RegistryGate)
        {
            if (!Connections.TryGetValue(nodeId, out var subscribers)) return;
            subscribers.Remove(socket);
            if (subscribers.Count == 0) Connections.Remove(nodeId);
        }
        // The accepting request owns/disposes the socket. A removed send gate remains
        // alive only while an already-started broadcast holds its managed reference.
    }

    public static async Task<OASISResult<int>> BroadcastAsync(string nodeId, object payload)
    {
        KeyValuePair<WS, SemaphoreSlim>[] subscribers;
        lock (RegistryGate)
            subscribers = Connections.TryGetValue(nodeId, out var group) ? group.ToArray() : [];

        var result = new OASISResult<int>();
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        var deliveries = await Task.WhenAll(subscribers.Select(async subscriber =>
        {
            var entered = false;
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            try
            {
                await subscriber.Value.WaitAsync(deadline.Token);
                entered = true;
                if (subscriber.Key.State != WebSocketState.Open)
                {
                    Unregister(nodeId, subscriber.Key);
                    return (Sent: false, Error: (Exception)null);
                }
                await subscriber.Key.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, deadline.Token);
                return (Sent: true, Error: (Exception)null);
            }
            catch (Exception ex) when (ex is WebSocketException || ex is OperationCanceledException || ex is ObjectDisposedException)
            {
                Unregister(nodeId, subscriber.Key);
                subscriber.Key.Abort();
                return (Sent: false, Error: ex);
            }
            finally { if (entered) subscriber.Value.Release(); }
        }));
        result.Result = deliveries.Count(delivery => delivery.Sent);
        result.Exception = deliveries.FirstOrDefault(delivery => delivery.Error != null).Error;
        if (result.Exception != null)
        {
            result.IsWarning = true;
            result.Message = "Node state was accepted, but a live subscriber disconnected or exceeded its delivery deadline.";
        }
        return result;
    }

    public static int ConnectionCount(string nodeId)
    {
        lock (RegistryGate)
            return Connections.TryGetValue(nodeId, out var subscribers)
                ? subscribers.Keys.Count(socket => socket.State == WebSocketState.Open) : 0;
    }
}
