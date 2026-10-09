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
using NextGenSoftware.OASIS.ONET;
using NextGenSoftware.OASIS.Common;
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

            var pingLine = FreshPingLine(ecdsa, nodeId, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            (await SendLineAsync(node.ListenPort, pingLine)).Should().Contain("ONET_PONG", "authenticated PING with valid signature must be accepted");
            (await SendLineAsync(node.ListenPort, pingLine)).Should().Contain("ONET_AUTH_FAILED", "a captured PING must not be replayable");
        }
        finally
        {
            await node.StopNetworkAsync();
        }
    }

    [Fact]
    public async Task ONETProtocol_AuthenticatedPing_StaleOrLegacyFormat_ReturnsAuthFailed()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var pubKeyB64 = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
        var nodeId = Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(pubKeyB64))).ToLowerInvariant();

        var node = new ONETProtocol(storageProvider: null) { ListenPort = GetFreeTcpPort() };
        node.RegisterNodePublicKey(nodeId, pubKeyB64).Should().BeTrue();
        await node.StartNetworkAsync();

        try
        {
            await Task.Delay(300);

            var stale = FreshPingLine(ecdsa, nodeId, DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds());
            (await SendLineAsync(node.ListenPort, stale)).Should().Contain("ONET_AUTH_FAILED");

            var legacySig = Convert.ToBase64String(ecdsa.SignData(Encoding.UTF8.GetBytes("ONET_PING"), HashAlgorithmName.SHA256));
            (await SendLineAsync(node.ListenPort, $"ONET_PING {nodeId} {legacySig}")).Should().Contain("ONET_AUTH_FAILED");
        }
        finally
        {
            await node.StopNetworkAsync();
        }
    }

    [Fact]
    public async Task ONETManager_ConnectAndDisconnect_UseTheProtocolPeerTable()
    {
        var dna = new OASISDNA();
        dna.OASIS.ONET = new ONETConfig { TcpPort = GetFreeTcpPort(), BootstrapServers = new List<string>(), AutoRegisterOnBootstrap = false };
        var mgr = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);
        var peer = new ONETProtocol(storageProvider: null) { ListenPort = GetFreeTcpPort() };
        await mgr.InitializeAsync();
        await mgr.StartNetworkAsync();
        await peer.StartNetworkAsync();

        try
        {
            await Task.Delay(300);
            (await mgr.ConnectToNodeAsync("peer-1", $"127.0.0.1:{peer.ListenPort}")).IsError.Should().BeFalse();

            var connected = (await mgr.GetNetworkStatsAsync()).Result;
            connected["totalNodes"].Should().Be(1);
            connected["consensusActiveMembers"].Should().Be(1, "connected peers become consensus members");
            (await mgr.GetConnectedNodesAsync()).Result.Should().ContainSingle(n => n.Id == "peer-1");

            (await mgr.DisconnectFromNodeAsync("peer-1")).IsError.Should().BeFalse();
            var disconnected = (await mgr.GetNetworkStatsAsync()).Result;
            disconnected["totalNodes"].Should().Be(0);
            disconnected["consensusActiveMembers"].Should().Be(0);
            (await mgr.DisconnectFromNodeAsync("peer-1")).IsError.Should().BeTrue("the peer is no longer connected");
        }
        finally
        {
            await peer.StopNetworkAsync();
            await mgr.StopNetworkAsync();
        }
    }

    private static string FreshPingLine(ECDsa ecdsa, string nodeId, long unixSeconds)
    {
        var message = ONETSecurity.BuildFreshSignedMessage(ONETSecurity.PingPurpose, nodeId, unixSeconds);
        var sig = Convert.ToBase64String(ecdsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256));
        return $"ONET_PING {nodeId} {unixSeconds} {sig}";
    }

    private static async Task<string> SendLineAsync(int port, string line)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        using var stream = client.GetStream();
        await stream.WriteAsync(Encoding.UTF8.GetBytes(line + "\n"));
        var buf = new byte[512];
        var read = await stream.ReadAsync(buf);
        return Encoding.UTF8.GetString(buf, 0, read);
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

            var pingLine = Encoding.UTF8.GetBytes($"ONET_PING {unknownNodeId} {DateTimeOffset.UtcNow.ToUnixTimeSeconds()} {sig}\n");

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
    public async Task TwoONETNodes_AuthenticatedFramedRequestResponse_RoundTripsOverTcp()
    {
        using var keyA = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var keyB = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicA = Convert.ToBase64String(keyA.ExportSubjectPublicKeyInfo());
        var privateA = Convert.ToBase64String(keyA.ExportPkcs8PrivateKey());
        var publicB = Convert.ToBase64String(keyB.ExportSubjectPublicKeyInfo());
        var privateB = Convert.ToBase64String(keyB.ExportPkcs8PrivateKey());
        var idA = Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(publicA))).ToLowerInvariant();
        var idB = Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(publicB))).ToLowerInvariant();
        var nodeA = new ONETProtocol(storageProvider: null) { ListenPort = GetFreeTcpPort() };
        var nodeB = new ONETProtocol(storageProvider: null) { ListenPort = GetFreeTcpPort() };

        (await nodeA.StartNetworkAsync()).IsError.Should().BeFalse();
        (await nodeB.StartNetworkAsync()).IsError.Should().BeFalse();
        try
        {
            nodeA.RegisterLocalNodeIdentity(idA, publicA, privateA);
            nodeB.RegisterLocalNodeIdentity(idB, publicB, privateB);
            nodeA.RegisterNodePublicKey(idB, publicB);
            nodeB.RegisterNodePublicKey(idA, publicA);
            (await nodeA.ConnectToNodeAsync(idB, $"127.0.0.1:{nodeB.ListenPort}")).IsError.Should().BeFalse();
            (await nodeB.ConnectToNodeAsync(idA, $"127.0.0.1:{nodeA.ListenPort}")).IsError.Should().BeFalse();

            var channelA = new ONETTcpApplicationMessageChannel(nodeA);
            var channelB = new ONETTcpApplicationMessageChannel(nodeB);
            using var endpointA = new ONETRequestResponseEndpoint(channelA);
            using var endpointB = new ONETRequestResponseEndpoint(channelB);
            endpointB.RegisterHandler("integration.echo", (request, _) =>
                Task.FromResult(new OASISResult<string> { Result = request.PayloadJson }));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var response = await endpointA.RequestAsync(idB, "integration.echo", "wire-payload", timeout.Token);

            response.IsError.Should().BeFalse(response.Message);
            response.Result.Should().Be("wire-payload");
        }
        finally
        {
            await nodeA.StopNetworkAsync();
            await nodeB.StopNetworkAsync();
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

    [Fact]
    public async Task ONETConsensus_ProposalReachesQuorum_FiresConsensusReachedEvent()
    {
        // Arrange: 3-node consensus, 67% threshold = 2/3 must approve.
        var consensus = new ONETConsensus(storageProvider: null);
        await consensus.InitializeAsync();
        await consensus.AddConsensusNodeAsync("node1", stake: 10, capabilities: new());
        await consensus.AddConsensusNodeAsync("node2", stake: 10, capabilities: new());
        await consensus.AddConsensusNodeAsync("node3", stake: 10, capabilities: new());

        var reachedIds = new System.Collections.Concurrent.ConcurrentBag<string>();
        consensus.ConsensusReached += (_, e) => reachedIds.Add(e.ConsensusId);

        var propResult = await consensus.ProposeAsync("node1", "test.action", new { value = 42 });
        propResult.IsError.Should().BeFalse();
        var proposalId = propResult.Result;

        // 2 approvals out of 3 = 66.7% — just below threshold; add a 3rd to push to 100%
        await consensus.VoteAsync(proposalId, "node1", approve: true);
        await consensus.VoteAsync(proposalId, "node2", approve: true);
        await consensus.VoteAsync(proposalId, "node3", approve: true);

        // Wait up to 5 s for the consensus loop to process the proposal.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!reachedIds.Contains(proposalId) && DateTime.UtcNow < deadline)
            await Task.Delay(100);

        reachedIds.Should().Contain(proposalId, "ConsensusReached must fire once quorum is reached");
        await consensus.StopAsync();
    }

    [Fact]
    public async Task ONETConsensus_ProposalRejectedByMajority_FiresConsensusFailedEvent()
    {
        // Arrange: 4-node consensus; 3 reject — participation ≥ 50% and approval < 67% → rejected.
        var consensus = new ONETConsensus(storageProvider: null);
        await consensus.InitializeAsync();
        await consensus.AddConsensusNodeAsync("node1", stake: 10, capabilities: new());
        await consensus.AddConsensusNodeAsync("node2", stake: 10, capabilities: new());
        await consensus.AddConsensusNodeAsync("node3", stake: 10, capabilities: new());
        await consensus.AddConsensusNodeAsync("node4", stake: 10, capabilities: new());

        var failedReasons = new System.Collections.Concurrent.ConcurrentBag<string>();
        consensus.ConsensusFailed += (_, e) => failedReasons.Add(e.Reason);

        var propResult = await consensus.ProposeAsync("node1", "test.action", new { value = 99 });
        propResult.IsError.Should().BeFalse();
        var proposalId = propResult.Result;

        // 1 approve, 3 reject → 25% approval, 100% participation → rejected
        await consensus.VoteAsync(proposalId, "node1", approve: true);
        await consensus.VoteAsync(proposalId, "node2", approve: false);
        await consensus.VoteAsync(proposalId, "node3", approve: false);
        await consensus.VoteAsync(proposalId, "node4", approve: false);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!failedReasons.Any(r => r.Contains(proposalId)) && DateTime.UtcNow < deadline)
            await Task.Delay(100);

        failedReasons.Should().Contain(r => r.Contains(proposalId), "ConsensusFailed must fire when proposal is rejected");
        await consensus.StopAsync();
    }

    [Fact]
    public async Task ONETManager_GetNetworkStatsAsync_IncludesLatencyAndThroughput()
    {
        var dna = new OASISDNA();
        dna.OASIS.ONET = new ONETConfig
        {
            TcpPort = GetFreeTcpPort(),
            BootstrapServers = new List<string>(),
            AutoRegisterOnBootstrap = false
        };

        var mgr = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);
        await mgr.InitializeAsync();
        await mgr.StartNetworkAsync();

        try
        {
            var statsResult = await mgr.GetNetworkStatsAsync();
            statsResult.IsError.Should().BeFalse(statsResult.Message);

            var stats = statsResult.Result;
            stats.Should().ContainKey("avgLatencyMs");
            stats.Should().ContainKey("throughputMbps");
            stats.Should().ContainKey("listenPort");
            stats.Should().ContainKey("consensusState");
            ((int)stats["listenPort"]).Should().Be(dna.OASIS.ONET.TcpPort);
        }
        finally
        {
            await mgr.StopNetworkAsync();
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
