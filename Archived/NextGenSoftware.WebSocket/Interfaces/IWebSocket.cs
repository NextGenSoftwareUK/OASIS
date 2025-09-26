using NextGenSoftware.Logging.Interfaces;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace NextGenSoftware.WebSocket.Interfaces
{
    public interface IWebSocket
    {
        ClientWebSocket ClientWebSocket { get; set; }
        WebSocketConfig Config { get; set; }
        Uri EndPoint { get; set; }
        ILogger Logger { get; set; }
        WebSocketState State { get; }

        event WebSocket.Connected OnConnected;
        event WebSocket.DataReceived OnDataReceived;
        event WebSocket.DataSent OnDataSent;
        event WebSocket.Disconnected OnDisconnected;
        event WebSocket.Error OnError;

        Task ConnectAsync(string endpoint);
        Task ConnectAsync(Uri endpoint);
        Task DisconnectAsync();
        Task SendRawDataAsync(byte[] data);
    }
}