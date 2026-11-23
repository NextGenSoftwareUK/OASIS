using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using WS = System.Net.WebSockets.WebSocket;
using WebSocketMessageType = System.Net.WebSockets.WebSocketMessageType;
using WebSocketState = System.Net.WebSockets.WebSocketState;
using WebSocketException = System.Net.WebSockets.WebSocketException;
using WebSocketCloseStatus = System.Net.WebSockets.WebSocketCloseStatus;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Hubs;

/// <summary>
/// Real-time push for messaging over native WebSockets at <c>/ws/messaging?ticket=...</c>.
/// A browser cannot send an Authorization header on a WebSocket, and a bearer token must not travel in a URL,
/// so the client first calls <c>POST api/messaging/realtime-ticket</c> (authenticated) and connects with the
/// returned single-use, 30-second ticket.
/// Push reaches sockets connected to THIS server instance. Clients catch up on anything they missed (another
/// instance, a dropped socket, being offline) with the durable <c>api/messaging/sync</c> cursor, which is the
/// source of truth; push only makes delivery immediate.
/// </summary>
public static class MessagingRealtimeHub
{
    private static readonly TimeSpan TicketLifetime = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan TypingInterval = TimeSpan.FromSeconds(1);
    private const int MaxClientFrameBytes = 4096;
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private sealed class Connection
    {
        public WS Socket;
        public readonly SemaphoreSlim SendLock = new(1, 1);
    }

    private static readonly ConcurrentDictionary<string, (Guid AvatarId, DateTime ExpiresUtc)> Tickets = new();
    private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Connection>> Connections = new();
    private static readonly ConcurrentDictionary<Guid, DateTime> LastTyping = new();
    private static int _subscribed;

    /// <summary>Hooks the manager's events once. Idempotent; called before any socket can exist.</summary>
    public static void EnsureSubscribed(MessagingManager manager)
    {
        if (Interlocked.Exchange(ref _subscribed, 1) == 1) return;
        manager.MessageStored += (message, recipients) => _ = PublishMessageAsync(message, recipients);
        manager.ReceiptsChanged += (message, byAvatarId, state) => _ = PublishReceiptAsync(message, byAvatarId, state);
    }

    public static string IssueTicket(Guid avatarId)
    {
        var now = DateTime.UtcNow;
        foreach (var expired in Tickets.Where(t => t.Value.ExpiresUtc < now).Select(t => t.Key).ToList())
            Tickets.TryRemove(expired, out _);
        var bytes = RandomNumberGenerator.GetBytes(32);
        var ticket = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Tickets[ticket] = (avatarId, now + TicketLifetime);
        return ticket;
    }

    /// <summary>Single use: a redeemed or expired ticket never works again.</summary>
    public static bool TryRedeemTicket(string ticket, out Guid avatarId)
    {
        avatarId = Guid.Empty;
        if (string.IsNullOrEmpty(ticket) || !Tickets.TryRemove(ticket, out var entry) || entry.ExpiresUtc < DateTime.UtcNow)
            return false;
        avatarId = entry.AvatarId;
        return true;
    }

    public static bool IsOnline(Guid avatarId) =>
        Connections.TryGetValue(avatarId, out var sockets) && sockets.Values.Any(c => c.Socket.State == WebSocketState.Open);

    public static async Task HandleAsync(HttpContext context)
    {
        if (!TryRedeemTicket(context.Request.Query["ticket"], out var avatarId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var id = Guid.NewGuid();
        var connection = new Connection { Socket = socket };
        Connections.GetOrAdd(avatarId, _ => new ConcurrentDictionary<Guid, Connection>())[id] = connection;
        try
        {
            var buffer = new byte[MaxClientFrameBytes];
            while (socket.State == WebSocketState.Open)
            {
                var received = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (received.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                    break;
                }
                if (!received.EndOfMessage)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "Frame too large", CancellationToken.None);
                    break;
                }
                if (received.MessageType == WebSocketMessageType.Text)
                    await HandleClientFrameAsync(avatarId, Encoding.UTF8.GetString(buffer, 0, received.Count));
            }
        }
        catch (WebSocketException) { }
        finally
        {
            if (Connections.TryGetValue(avatarId, out var sockets))
            {
                sockets.TryRemove(id, out _);
                if (sockets.IsEmpty) Connections.TryRemove(avatarId, out _);
            }
        }
    }

    private static async Task HandleClientFrameAsync(Guid avatarId, string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("type", out var type) || type.GetString() != "typing")
                return;
            var now = DateTime.UtcNow;
            if (LastTyping.TryGetValue(avatarId, out var last) && now - last < TypingInterval)
                return;
            LastTyping[avatarId] = now;

            Guid conversationId = Guid.Empty;
            List<Guid> recipients;
            if (root.TryGetProperty("conversationId", out var conv) && Guid.TryParse(conv.GetString(), out conversationId) && conversationId != Guid.Empty)
            {
                var group = await MessagingManager.Instance.GetGroupAsync(avatarId, conversationId);
                if (group.IsError) return;
                recipients = group.Result.ParticipantIds.Where(p => p != avatarId).ToList();
            }
            else if (root.TryGetProperty("toAvatarId", out var to) && Guid.TryParse(to.GetString(), out var toAvatar) && toAvatar != Guid.Empty)
            {
                conversationId = MessagingConversation.DirectId(avatarId, toAvatar);
                recipients = new List<Guid> { toAvatar };
            }
            else return;
            foreach (var recipient in recipients)
                await PublishAsync(recipient, new { type = "typing", fromAvatarId = avatarId, conversationId });
        }
        catch (JsonException) { }
    }

    private static async Task PublishMessageAsync(Message message, IReadOnlyList<Guid> recipients)
    {
        // The sender's other devices also need the message, so it goes to them too.
        foreach (var avatarId in recipients.Append(message.FromAvatarId).Distinct())
            await PublishAsync(avatarId, new { type = "message", message });
    }

    private static Task PublishReceiptAsync(Message message, Guid byAvatarId, MessageDeliveryState state) =>
        PublishAsync(message.FromAvatarId, new
        {
            type = "receipt", messageId = message.Id, conversationId = message.EffectiveConversationId,
            state = state.ToString(), byAvatarId
        });

    /// <summary>Sends to every open socket of the avatar. Never throws: a dead socket is dropped.</summary>
    public static async Task PublishAsync(Guid avatarId, object payload)
    {
        if (!Connections.TryGetValue(avatarId, out var sockets) || sockets.IsEmpty) return;
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, Json);
        foreach (var pair in sockets.ToList())
        {
            var connection = pair.Value;
            if (connection.Socket.State != WebSocketState.Open)
            {
                sockets.TryRemove(pair.Key, out _);
                continue;
            }
            await connection.SendLock.WaitAsync();
            try { await connection.Socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None); }
            catch (Exception ex) when (ex is WebSocketException or ObjectDisposedException or InvalidOperationException)
            {
                sockets.TryRemove(pair.Key, out _);
            }
            finally { connection.SendLock.Release(); }
        }
    }
}
