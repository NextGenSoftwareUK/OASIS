#Requires -Version 7.0
# One anonymous read subscription. All configuration arrives as public JSON on stdin.
# No relay publication, login, alternative endpoint or implicit retry is permitted.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$inputValue = [Console]::In.ReadToEnd() | ConvertFrom-Json
if ($inputValue.relay -cne 'wss://relay.damus.io/') { throw 'Explicit qualification relay required.' }
$core = Join-Path $root 'OASIS Architecture/NextGenSoftware.OASIS.API.Core/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.dll'
[Reflection.Assembly]::LoadFrom($core) | Out-Null
[NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeConnectionGuard]::AllowPrivateOverride = $false
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
public static class NostrQualificationTransport
{
    public static async Task<string[]> Read(string relay, string request, string subscription, HttpMessageHandler handler)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var invoker = new HttpMessageInvoker(handler, true);
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(relay), invoker, deadline.Token);
        await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(request)), WebSocketMessageType.Text, true, deadline.Token);
        var messages = new System.Collections.Generic.List<string>();
        var buffer = new byte[8192];
        int total = 0;
        // EOSE is the completion marker, not the first EVENT frame.
        for (int frames = 0; frames < 64;)
        {
            using var message = new MemoryStream();
            WebSocketReceiveResult received;
            do
            {
                if (++frames > 64) throw new InvalidDataException("Frame budget exceeded.");
                received = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), deadline.Token);
                if (received.MessageType != WebSocketMessageType.Text) throw new InvalidDataException("Text messages required.");
                total += received.Count;
                if (total > 262144 || message.Length + received.Count > 65536) throw new InvalidDataException("Response budget exceeded.");
                message.Write(buffer, 0, received.Count);
            } while (!received.EndOfMessage);
            string text = new UTF8Encoding(false, true).GetString(message.ToArray());
            using var json = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 24 });
            var row = json.RootElement;
            if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() < 2 ||
                row[0].ValueKind != JsonValueKind.String || row[1].ValueKind != JsonValueKind.String)
                throw new InvalidDataException("Invalid subscription envelope.");
            string kind = row[0].GetString();
            if (row[1].GetString() != subscription) throw new InvalidDataException("Unexpected subscription or relay notice.");
            if (kind == "EVENT" && row.GetArrayLength() == 3)
            {
                if (messages.Count == 8) throw new InvalidDataException("Event budget exceeded.");
                messages.Add(text);
            }
            else if (kind == "EOSE" && row.GetArrayLength() == 2)
            {
                // Disposal closes this one-shot socket; there is no live subscription consumer.
                return messages.ToArray();
            }
            else throw new InvalidDataException("Unexpected relay message; no partial result.");
        }
        throw new InvalidDataException("EOSE missing within frame budget.");
    }
}
'@
$handler = [NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeConnectionGuard]::CreateGuardedHttpHandler()
([Net.Http.SocketsHttpHandler]$handler).UseCookies = $false
$request = ConvertTo-Json -InputObject @('REQ', $inputValue.subscription, $inputValue.filter) -Depth 12 -Compress
$messages = [NostrQualificationTransport]::Read($inputValue.relay, $request, $inputValue.subscription, $handler).GetAwaiter().GetResult()
ConvertTo-Json -InputObject @($messages) -Compress
