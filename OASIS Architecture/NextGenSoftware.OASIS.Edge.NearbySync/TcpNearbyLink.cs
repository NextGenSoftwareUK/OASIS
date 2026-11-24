using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>
    /// Nearby link over TCP: same-LAN devices, a phone hotspot, and Wi-Fi Direct groups (which present as an IP
    /// network). Frames are length-prefixed. Peers are connected explicitly with <see cref="ConnectAsync"/>
    /// (host and port learned from QR/NFC pairing or mDNS supplied by the platform layer).
    /// </summary>
    public sealed class TcpNearbyLink : INearbyLink
    {
        private const int MaximumWireFrame = 1024 * 1024;
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<string, Connection> _connections = new ConcurrentDictionary<string, Connection>();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

        private sealed class Connection
        {
            public TcpClient Client;
            public Stream Stream;
            public readonly SemaphoreSlim WriteLock = new SemaphoreSlim(1, 1);
        }

        public TcpNearbyLink(NearbyLinkKind kind = NearbyLinkKind.Lan, int maximumFrameSize = 16384, int listenPort = 0)
        {
            Kind = kind;
            MaximumFrameSize = maximumFrameSize;
            _listener = new TcpListener(IPAddress.Any, listenPort);
        }

        public NearbyLinkKind Kind { get; }
        public int MaximumFrameSize { get; }
        public IReadOnlyCollection<string> ConnectedPeerIds => _connections.Keys.ToList();
        public event Action<string, byte[]> FrameReceived;

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public void Start()
        {
            _listener.Start();
            _ = Task.Run(AcceptLoopAsync);
        }

        public async Task<string> ConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            var client = new TcpClient();
            await client.ConnectAsync(host, port).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return Register(client);
        }

        private async Task AcceptLoopAsync()
        {
            while (!_lifetime.IsCancellationRequested)
            {
                try { Register(await _listener.AcceptTcpClientAsync().ConfigureAwait(false)); }
                catch (ObjectDisposedException) { return; }
                catch (SocketException) { if (_lifetime.IsCancellationRequested) return; }
            }
        }

        private string Register(TcpClient client)
        {
            client.NoDelay = true;
            var id = client.Client.RemoteEndPoint.ToString();
            var connection = new Connection { Client = client, Stream = client.GetStream() };
            _connections[id] = connection;
            _ = Task.Run(() => ReadLoopAsync(id, connection));
            return id;
        }

        private async Task ReadLoopAsync(string id, Connection connection)
        {
            var header = new byte[4];
            try
            {
                while (!_lifetime.IsCancellationRequested)
                {
                    if (!await ReadExactAsync(connection.Stream, header, 4).ConfigureAwait(false))
                        break;
                    var length = BitConverter.ToInt32(header, 0);
                    if (length <= 0 || length > MaximumWireFrame)
                        break;
                    var frame = new byte[length];
                    if (!await ReadExactAsync(connection.Stream, frame, length).ConfigureAwait(false))
                        break;
                    FrameReceived?.Invoke(id, frame);
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            finally
            {
                _connections.TryRemove(id, out _);
                connection.Client.Dispose();
            }
        }

        private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, int count)
        {
            var read = 0;
            while (read < count)
            {
                var n = await stream.ReadAsync(buffer, read, count - read).ConfigureAwait(false);
                if (n == 0) return false;
                read += n;
            }
            return true;
        }

        public async Task SendAsync(string peerId, byte[] frame, CancellationToken cancellationToken)
        {
            if (frame.Length > MaximumFrameSize)
                throw new ArgumentException($"Frame exceeds the {MaximumFrameSize}-byte link limit.", nameof(frame));
            if (!_connections.TryGetValue(peerId, out var connection))
                throw new InvalidOperationException($"Peer '{peerId}' is not connected.");
            await connection.WriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await connection.Stream.WriteAsync(BitConverter.GetBytes(frame.Length), 0, 4, cancellationToken).ConfigureAwait(false);
                await connection.Stream.WriteAsync(frame, 0, frame.Length, cancellationToken).ConfigureAwait(false);
                await connection.Stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"Peer '{peerId}' disconnected.", ex);
            }
            finally { connection.WriteLock.Release(); }
        }

        public void Dispose()
        {
            _lifetime.Cancel();
            _listener.Stop();
            foreach (var c in _connections.Values) c.Client.Dispose();
            _connections.Clear();
        }
    }
}
