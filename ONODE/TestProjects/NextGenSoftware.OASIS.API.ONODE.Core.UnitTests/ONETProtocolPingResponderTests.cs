using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    /// <summary>
    /// Regression test for the missing ONET_PING/ONET_PONG server-side responder. ONETRouting's
    /// TestNodeConnectivityAsync (and ONETProtocol's own ACK wait in PerformRealNetworkTransmissionAsync)
    /// already implement a real TCP client that sends ONET_PING and expects ONET_PONG back, but nothing in
    /// the codebase ever listened for incoming connections to answer it - so connectivity tests against a
    /// real, healthy ONET node always failed. This test starts the real responder and connects to it with a
    /// plain TcpClient to confirm it now answers correctly.
    /// </summary>
    public class ONETProtocolPingResponderTests
    {
        [Fact]
        public async Task PingResponder_RealTcpConnection_RespondsWithOnetPong()
        {
            var protocol = new ONETProtocol(storageProvider: null);
            var port = GetFreeTcpPort();
            protocol.GetType().GetProperty("ListenPort")!.SetValue(protocol, port);

            var startMethod = protocol.GetType().GetMethod("StartPingResponder", BindingFlags.NonPublic | BindingFlags.Instance);
            var stopMethod = protocol.GetType().GetMethod("StopPingResponder", BindingFlags.NonPublic | BindingFlags.Instance);

            startMethod!.Invoke(protocol, null);

            try
            {
                // Give the background accept-loop a moment to start listening.
                await Task.Delay(200);

                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, port);
                using var stream = client.GetStream();

                var ping = Encoding.UTF8.GetBytes("ONET_PING\n");
                await stream.WriteAsync(ping, 0, ping.Length);

                var buffer = new byte[256];
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                var response = Encoding.UTF8.GetString(buffer, 0, read);

                response.Should().Contain("ONET_PONG");
            }
            finally
            {
                stopMethod!.Invoke(protocol, null);
            }
        }

        private static int GetFreeTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
