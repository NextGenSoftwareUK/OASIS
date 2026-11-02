using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;

namespace NextGenSoftware.OASIS.API.Providers.NostrOASIS;

internal static class NostrPublicRelayRead
{
    internal static async Task<IReadOnlyList<string>> ReadAsync(Uri relay, IReadOnlyList<string> ids, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        using var invoker = new HttpMessageInvoker(handler, true);
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(relay, invoker, deadline.Token);
        return await ReadConnectedAsync(socket, ids, "cosmic-" + Guid.NewGuid().ToString("N"), deadline.Token);
    }

    internal static async Task<IReadOnlyList<string>> ReadConnectedAsync(WebSocket socket, IReadOnlyList<string> ids,
        string subscription, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        string request = JsonSerializer.Serialize(new object[] { "REQ", subscription,
            new { ids, kinds = new[] { 1 }, limit = ids.Count } });
        await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(request)), WebSocketMessageType.Text, true, token);
        var events = new List<string>();
        var buffer = new byte[8192];
        int frames = 0, total = 0;
        while (frames < 64)
        {
            using var message = new MemoryStream();
            WebSocketReceiveResult received;
            do
            {
                if (++frames > 64) throw new InvalidDataException("Relay frame budget exceeded.");
                received = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                if (received.MessageType != WebSocketMessageType.Text)
                    throw new InvalidDataException("Relay closed or returned a non-text message before EOSE.");
                total += received.Count;
                if (total > 262144 || message.Length + received.Count > 65536)
                    throw new InvalidDataException("Relay response budget exceeded.");
                message.Write(buffer, 0, received.Count);
            } while (!received.EndOfMessage);
            string text = new UTF8Encoding(false, true).GetString(message.ToArray());
            using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 24 });
            var frame = document.RootElement;
            if (frame.ValueKind != JsonValueKind.Array || frame.GetArrayLength() < 2 ||
                frame[0].ValueKind != JsonValueKind.String || frame[1].ValueKind != JsonValueKind.String ||
                frame[1].GetString() != subscription)
                throw new InvalidDataException("Unexpected relay subscription/envelope; no partial selection.");
            if (frame[0].GetString() == "EVENT" && frame.GetArrayLength() == 3)
            {
                if (events.Count == 8) throw new InvalidDataException("Relay event budget exceeded.");
                events.Add(frame[2].GetRawText());
            }
            else if (frame[0].GetString() == "EOSE" && frame.GetArrayLength() == 2)
            {
                token.ThrowIfCancellationRequested();
                return events.AsReadOnly();
            }
            else throw new InvalidDataException("Relay notice/auth/closed/unexpected message; no partial selection.");
        }
        throw new InvalidDataException("EOSE missing within relay frame budget.");
    }
}
