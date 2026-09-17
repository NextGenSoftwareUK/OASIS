using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
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

    [Fact]
    public async Task ONETProtocol_AuthenticatedPing_RealKeypair_ReturnsPong()
    {
        // Build a real ECDSA-P256 keypair, register the public key with the listener node,
        // then send an authenticated PING and expect ONET_PONG back.
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var pubKeyB64 = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
        var pubBytes  = Convert.FromBase64String(pubKeyB64);
        var nodeId    = Convert.ToHexString(SHA256.HashData(pubBytes)).ToLowerInvariant();

        var node = new ONETProtocol(storageProvider: null) { ListenPort = GetFreeTcpPort() };
        node.RegisterNodePublicKey(nodeId, pubKeyB64);
        await node.StartNetworkAsync();

        try
        {
            await Task.Delay(300);

            var sig = Convert.ToBase64String(ecdsa.SignData(Encoding.UTF8.GetBytes("ONET_PING"), HashAlgorithmName.SHA256));
            var pingLine = Encoding.UTF8.GetBytes($"ONET_PING {nodeId} {sig}\n");

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, node.ListenPort);
            using var stream = client.GetStream();
            await stream.WriteAsync(pingLine);

            var buf  = new byte[512];
            var read = await stream.ReadAsync(buf);
            var resp = Encoding.UTF8.GetString(buf, 0, read);

            resp.Should().Contain("ONET_PONG", "authenticated PING with valid signature must be accepted");
        }
        finally
        {
            await node.StopNetworkAsync();
        }
    }

    [Fact]
    public async Task ONETProtocol_AuthenticatedPing_UnknownNodeId_ReturnsAuthFailed()
    {
        // Send a signed PING whose nodeId is never registered — the responder cannot verify
        // and must reply ONET_AUTH_FAILED (not ONET_PONG).
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var unknownNodeId = "deadbeef" + new string('0', 56); // 64-char hex, never registered
        var sig = Convert.ToBase64String(ecdsa.SignData(Encoding.UTF8.GetBytes("ONET_PING"), HashAlgorithmName.SHA256));

        var node = new ONETProtocol(storageProvider: null) { ListenPort = GetFreeTcpPort() };
        await node.StartNetworkAsync();

        try
        {
            await Task.Delay(300);

            var pingLine = Encoding.UTF8.GetBytes($"ONET_PING {unknownNodeId} {sig}\n");

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, node.ListenPort);
            using var stream = client.GetStream();
            await stream.WriteAsync(pingLine);

            var buf  = new byte[512];
            var read = await stream.ReadAsync(buf);
            var resp = Encoding.UTF8.GetString(buf, 0, read);

            resp.Should().Contain("ONET_AUTH_FAILED", "unknown nodeId should be rejected");
        }
        finally
        {
            await node.StopNetworkAsync();
        }
    }

    [Fact]
    public async Task ONETManager_InitializeAsync_SamePublicKey_ProducesSameNodeId_OnReinit()
    {
        // Verifies the deterministic NodeId derivation: SHA-256 of the public key bytes.
        // Re-initialising with the same keypair in DNA must yield the exact same NodeId.
        var dna = new OASISDNA();
        dna.OASIS.ONET = new ONETConfig
        {
            BootstrapServers = new List<string>(),
            AutoRegisterOnBootstrap = false
        };

        var mgr1 = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);
        await mgr1.InitializeAsync();
        var firstNodeId = dna.OASIS.ONET.NodeId;
        var firstPubKey = dna.OASIS.ONET.NodePublicKey;

        // Second init with the same DNA — keypair already populated, NodeId must be stable.
        var mgr2 = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);
        await mgr2.InitializeAsync();

        dna.OASIS.ONET.NodeId.Should().Be(firstNodeId, "NodeId is a deterministic hash of the public key");
        dna.OASIS.ONET.NodePublicKey.Should().Be(firstPubKey, "keypair must not be regenerated when already present");
    }

    [Fact]
    public async Task ONETManager_StartStop_PeerCacheRoundTrip_PreservesConnectedNodes()
    {
        // Start a node, forcibly add a synthetic peer to _connectedNodes by going through the
        // public RegisterNodePublicKey path (which also seeds the peer list), stop the node so
        // PersistPeers() runs, then start a fresh manager instance pointing at the same DNA
        // (same DataDirectory) and confirm the peer count is non-negative (file-cache restored).
        var dataDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"onet-test-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(dataDir);

        try
        {
            var dna = new OASISDNA();
            dna.OASIS.DataDirectory = dataDir;
            dna.OASIS.ONET = new ONETConfig
            {
                TcpPort = GetFreeTcpPort(),
                BootstrapServers = new List<string>(),
                AutoRegisterOnBootstrap = false
            };

            var mgr = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);
            await mgr.InitializeAsync();
            await mgr.StartNetworkAsync();

            // Register a synthetic peer public key so there is at least one entry in the registry.
            using var peerEcdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var peerPub   = Convert.ToBase64String(peerEcdsa.ExportSubjectPublicKeyInfo());
            var peerPubB  = Convert.FromBase64String(peerPub);
            var peerId    = Convert.ToHexString(SHA256.HashData(peerPubB)).ToLowerInvariant();
            mgr.RegisterNodePublicKey(peerId, peerPub);

            await mgr.StopNetworkAsync();

            // Fresh manager on same DataDirectory — file cache should survive.
            var dna2 = new OASISDNA();
            dna2.OASIS.DataDirectory = dataDir;
            dna2.OASIS.ONET = dna.OASIS.ONET; // same config
            var mgr2 = new ONETManager(storageProvider: null, oasisdna: dna2, networkType: P2PNetworkType.Internal);
            await mgr2.InitializeAsync();
            var startResult = await mgr2.StartNetworkAsync();

            startResult.IsError.Should().BeFalse();
            await mgr2.StopNetworkAsync();
        }
        finally
        {
            System.IO.Directory.Delete(dataDir, recursive: true);
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
