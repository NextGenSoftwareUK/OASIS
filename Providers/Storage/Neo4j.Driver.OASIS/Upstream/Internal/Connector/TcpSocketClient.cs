// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using static System.Security.Authentication.SslProtocols;

namespace Neo4j.Driver.Internal.Connector
{
    internal class TcpSocketClient : ITcpSocketClient
    {
        private Socket _client;
        private Stream _stream;
        private readonly object _connectionGate = new object();
        private readonly CancellationTokenSource _disconnect = new CancellationTokenSource();
        private readonly CancellationToken _disconnectToken;
        private int _disconnected;

        private readonly IHostResolver _resolver;
        private readonly bool _ipv6Enabled;
        private readonly EncryptionManager _encryptionManager;
        private readonly TimeSpan _connectionTimeout;
        private readonly bool _socketKeepAliveEnabled;
        private readonly ILogger _logger;

        public Stream ReadStream => _stream;
        public Stream WriteStream => _stream;

        public TcpSocketClient(SocketSettings socketSettings, ILogger logger = null)
        {
            Throw.ArgumentNullException.IfNull(socketSettings, nameof(socketSettings));
            Throw.ArgumentNullException.IfNull(socketSettings.HostResolver, nameof(SocketSettings.HostResolver));
            Throw.ArgumentNullException.IfNull(socketSettings.EncryptionManager,
                nameof(SocketSettings.EncryptionManager));

            _disconnectToken = _disconnect.Token;
            _logger = logger;
            _resolver = socketSettings.HostResolver;
            _encryptionManager = socketSettings.EncryptionManager;
            _ipv6Enabled = socketSettings.Ipv6Enabled;
            _connectionTimeout = socketSettings.ConnectionTimeout;
            _socketKeepAliveEnabled = socketSettings.SocketKeepAliveEnabled;
        }

        public async Task ConnectAsync(Uri uri)
        {
            var client = await ConnectSocketAsync(uri).ConfigureAwait(false);
            Stream stream;
            lock (_connectionGate)
            {
                _disconnectToken.ThrowIfCancellationRequested();
                _stream = stream = new NetworkStream(client);
            }
            if (_encryptionManager.UseTls)
            {
                try
                {
                    SslStream sslStream;
                    lock (_connectionGate)
                    {
                        _disconnectToken.ThrowIfCancellationRequested();
                        _stream = sslStream = CreateSecureStream(uri, stream);
                    }
                    await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        TargetHost = uri.Host,
                        EnabledSslProtocols = Tls12,
                        CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                    }, _disconnectToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_disconnectToken.IsCancellationRequested) { throw; }
                catch (Exception e)
                {
                    throw new SecurityException($"Failed to establish encrypted connection with server {uri}.", e);
                }
            }
            _disconnectToken.ThrowIfCancellationRequested();
        }

        private async Task<Socket> ConnectSocketAsync(Uri uri)
        {
            _disconnectToken.ThrowIfCancellationRequested();
            var innerErrors = new List<Exception>();
            var addresses = await _resolver.ResolveAsync(uri.Host, _disconnectToken).ConfigureAwait(false);
            _disconnectToken.ThrowIfCancellationRequested();
            foreach (var address in addresses)
            {
                _disconnectToken.ThrowIfCancellationRequested();
                try
                {
                    return await ConnectSocketAsync(address, uri.Port).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_disconnectToken.IsCancellationRequested) { throw; }
                catch (Exception e)
                {
                    var actualException = e is AggregateException aggregate ? aggregate.GetBaseException() : e;
                    innerErrors.Add(new IOException(
                        $"Failed to connect to server '{uri}' via IP address '{address}': {actualException.Message}",
                        actualException));
                }
            }
            throw new IOException(
                $"Failed to connect to server '{uri}' via IP addresses'{addresses.ToContentString()}' at port '{uri.Port}'.",
                new AggregateException(innerErrors));
        }

        internal async Task<Socket> ConnectSocketAsync(IPAddress address, int port)
        {
            var client = InitClient();
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(_disconnectToken);
            attempt.CancelAfter(_connectionTimeout);
            try
            {
                // Native cancellation joins connect completion; no detached connect task
                // can later reopen a connection after its owner has shut down.
                await client.ConnectAsync(address, port, attempt.Token).ConfigureAwait(false);
                _disconnectToken.ThrowIfCancellationRequested();
                return client;
            }
            catch
            {
                lock (_connectionGate)
                {
                    if (ReferenceEquals(_client, client)) _client = null;
                }
                client.Dispose();
                throw;
            }
        }

        public virtual Task DisconnectAsync()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) != 0) return Task.CompletedTask;
            try { _disconnect.Cancel(); }
            finally
            {
                Socket client;
                Stream stream;
                lock (_connectionGate)
                {
                    client = _client;
                    stream = _stream;
                    _client = null;
                    _stream = null;
                }
                // Preserve the transport's shutdown handshake before disposal.
                // Disposing a socket with unread Bolt bytes can send TCP RST.
                try
                {
                    if (client?.Connected == true) client.Shutdown(SocketShutdown.Both);
                }
                finally
                {
                    try { stream?.Dispose(); }
                    finally
                    {
                        client?.Dispose();
                        _disconnect.Dispose();
                    }
                }
            }
            return Task.CompletedTask;
        }

        private Socket InitClient()
        {
            lock (_connectionGate)
            {
                _disconnectToken.ThrowIfCancellationRequested();
                var client = _ipv6Enabled
                    ? new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp) { DualMode = true }
                    : new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.NoDelay = true;
                client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, _socketKeepAliveEnabled);
                _client = client;
                return client;
            }
        }

        private SslStream CreateSecureStream(Uri uri, Stream stream)
        {
            return new SslStream(stream, true,
                (sender, certificate, chain, errors) =>
                {
                    if (errors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
                    {
                        _logger?.Error(null, $"{GetType().Name}: Certificate not available.");
                        return false;
                    }

                    var trust = _encryptionManager.TrustManager.ValidateServerCertificate(uri,
                        new X509Certificate2(certificate.Export(X509ContentType.Cert)), chain, errors);

                    if (trust)
                    {
                        _logger?.Debug("Trust is established, resuming connection.");
                    }
                    else
                    {
                        _logger?.Error(null, "Trust not established, aborting communication.");
                    }

                    return trust;
                });
        }
    }
}
