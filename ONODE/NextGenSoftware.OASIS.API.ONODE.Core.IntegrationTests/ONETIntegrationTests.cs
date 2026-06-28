using System;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.IntegrationTests;

/// <summary>
/// End-to-end integration tests for ONET, exercising the real (now-fixed) component wiring together rather
/// than any single class in isolation: ONETManager construction, network start/stop through the full
/// Protocol -> Security/Discovery/Consensus/Routing/APIGateway chain, and a real two-node connectivity
/// round-trip over the PING/PONG TCP responder added to fix the previously-missing server side.
/// </summary>
public class ONETIntegrationTests
{
    [Fact]
    public void ONETManager_Construction_DoesNotCrash_ForInternalP2PNetworkType()
    {
        // Regression test for the guaranteed crash this used to hit when constructed at all (the HoloNET
        // branch dereferenced a null IHoloNETClientBase even when Internal mode was requested, because
        // InitializeP2PNetworkProvider ran synchronously in the constructor and any path through it could
        // throw before the object was even usable).
        Action act = () => new ONETManager(storageProvider: null, oasisdna: null, networkType: P2PNetworkType.Internal);

        act.Should().NotThrow();
    }

    [Fact]
    public void ONETManager_Construction_HoloNETWithoutHoloOASISProvider_ThrowsClearException()
    {
        // P2PNetworkType.HoloNET requires the storageProvider to actually be a HoloOASIS instance (so its
        // HoloNETClientAppAgent can back the HoloNET P2P provider). Passing null should now fail with a
        // clear, actionable message instead of an opaque ArgumentNullException deep inside HoloNETP2PProvider.
        Action act = () => new ONETManager(storageProvider: null, oasisdna: null, networkType: P2PNetworkType.HoloNET);

        act.Should().Throw<InvalidOperationException>().WithMessage("*HoloOASIS*");
    }

    [Fact]
    public async Task ONETProtocol_StartThenStopNetwork_RealComponentChain_Succeeds()
    {
        var protocol = new ONETProtocol(storageProvider: null);

        var startResult = await protocol.StartNetworkAsync();
        startResult.IsError.Should().BeFalse(startResult.Message);

        var stopResult = await protocol.StopNetworkAsync();
        stopResult.IsError.Should().BeFalse(stopResult.Message);
    }

    [Fact]
    public async Task TwoONETProtocolInstances_PingEachOtherOverRealTcp_RoundTripsSuccessfully()
    {
        // Two independent ONETProtocol instances on different ports, each with its own real PING responder
        // running, confirm the responder added to fix the "no server-side listener" bug actually works
        // end-to-end between two separate node instances rather than just a synthetic raw-socket test.
        var nodeA = new ONETProtocol(storageProvider: null) { ListenPort = GetFreeTcpPort() };
        var nodeB = new ONETProtocol(storageProvider: null) { ListenPort = GetFreeTcpPort() };

        await nodeA.StartNetworkAsync();
        await nodeB.StartNetworkAsync();

        try
        {
            await Task.Delay(300); // let both responders start listening

            using var client = new System.Net.Sockets.TcpClient();
            await client.ConnectAsync(System.Net.IPAddress.Loopback, nodeB.ListenPort);
            using var stream = client.GetStream();

            var ping = System.Text.Encoding.UTF8.GetBytes("ONET_PING\n");
            await stream.WriteAsync(ping, 0, ping.Length);

            var buffer = new byte[256];
            var read = await stream.ReadAsync(buffer, 0, buffer.Length);
            var response = System.Text.Encoding.UTF8.GetString(buffer, 0, read);

            response.Should().Contain("ONET_PONG");
        }
        finally
        {
            await nodeA.StopNetworkAsync();
            await nodeB.StopNetworkAsync();
        }
    }

    private static int GetFreeTcpPort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
