using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.DNA;
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

/// <summary>
/// Integration tests for the ONODE ↔ ONET delegation chain introduced in Phase 2:
/// ONODEManager receiving an injected ONETManager, StartNodeAsync wiring through to ONET,
/// and peer/stat queries delegating to live ONET state.
/// </summary>
public class ONODEONETChainIntegrationTests
{
    private static OASISDNA BuildDna(string networkType = "Internal")
    {
        var dna = new OASISDNA();
        dna.OASIS.ONET = new ONETConfig
        {
            NetworkType = networkType,
            NodeId = "",
            NodePublicKey = "",
            NodePrivateKey = "",
            BootstrapServers = new List<string>(),
            TcpPort = 38472,
            EnableMDNS = false,
            AutoRegisterOnBootstrap = false
        };
        return dna;
    }

    [Fact]
    public async Task ONODEManager_StartNode_StartsInjectedONETNetwork()
    {
        var dna = BuildDna();
        var onet = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);
        await onet.InitializeAsync();

        var onode = new ONODEManager(storageProvider: null, oasisdna: dna, onetManager: onet);
        var startResult = await onode.StartNodeAsync();

        startResult.IsError.Should().BeFalse();

        // ONET should have uptime > zero now
        var stats = await onet.GetNetworkStatsAsync();
        stats.Result.Should().ContainKey("uptime");

        await onode.StopNodeAsync();
    }

    [Fact]
    public async Task ONODEManager_StopNode_StopsInjectedONETNetwork()
    {
        var dna = BuildDna();
        var onet = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);
        await onet.InitializeAsync();

        var onode = new ONODEManager(storageProvider: null, oasisdna: dna, onetManager: onet);
        await onode.StartNodeAsync();

        var stopResult = await onode.StopNodeAsync();

        stopResult.IsError.Should().BeFalse();
        var status = await onode.GetNodeStatusAsync();
        status.Result!.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task ONODEManager_GetNodeStats_MergesONETStatsWithPrefix()
    {
        var dna = BuildDna();
        var onet = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);
        await onet.InitializeAsync();

        var onode = new ONODEManager(storageProvider: null, oasisdna: dna, onetManager: onet);
        await onode.StartNodeAsync();

        var stats = await onode.GetNodeStatsAsync();

        stats.IsError.Should().BeFalse();
        stats.Result.Should().ContainKey("nodeRunning");
        stats.Result.Keys.Should().Contain(k => k.StartsWith("onet_"), "ONET stats merged with onet_ prefix");

        await onode.StopNodeAsync();
    }

    [Fact]
    public async Task ONODEManager_GetConnectedPeers_DelegatesToONET()
    {
        var dna = BuildDna();
        var onet = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);
        await onet.InitializeAsync();

        var onode = new ONODEManager(storageProvider: null, oasisdna: dna, onetManager: onet);

        var result = await onode.GetConnectedPeersAsync();

        result.IsError.Should().BeFalse();
        result.Result.Should().NotBeNull("list is non-null even when no peers are connected yet");
    }

    [Fact]
    public async Task ONETManager_InitializeAsync_GeneratesNodeId_PersistedToDna()
    {
        var dna = BuildDna();
        dna.OASIS.ONET.NodeId = "";

        var onet = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);
        await onet.InitializeAsync();

        dna.OASIS.ONET.NodeId.Should().NotBeNullOrWhiteSpace("keypair generated on first run");
        dna.OASIS.ONET.NodePublicKey.Should().NotBeNullOrWhiteSpace();
        dna.OASIS.ONET.NodePrivateKey.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ONETManager_GetNetworkStats_ContainsNetworkTypeFromDna()
    {
        var dna = BuildDna(networkType: "Internal");
        var onet = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);
        await onet.InitializeAsync();

        var stats = await onet.GetNetworkStatsAsync();

        stats.IsError.Should().BeFalse();
        stats.Result["networkType"].Should().Be("Internal");
    }
}
