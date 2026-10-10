using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests;

public class ONETManagerUnitTests
{
    private static OASISDNA BuildDna(
        string networkType = "Internal",
        string nodeId = "",
        string nodePublicKey = "",
        string nodePrivateKey = "",
        List<string>? bootstrapServers = null)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS() };
        dna.OASIS.ONET = new ONETConfig
        {
            NetworkType = networkType,
            NodeId = nodeId,
            NodePublicKey = nodePublicKey,
            NodePrivateKey = nodePrivateKey,
            BootstrapServers = bootstrapServers ?? new List<string>(),
            TcpPort = 38470,
            EnableMDNS = true,
            AutoRegisterOnBootstrap = true
        };
        return dna;
    }

    [Fact]
    public void Constructor_WithNullDna_DoesNotThrow()
    {
        Action act = () => new ONETManager(storageProvider: null, oasisdna: null, networkType: P2PNetworkType.Internal);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithInternalNetworkType_InDna_DoesNotThrow()
    {
        var dna = BuildDna(networkType: "Internal");
        Action act = () => new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_HoloNETMode_WithNullProvider_ThrowsInvalidOperationException()
    {
        // HoloNET mode requires HoloOASIS storage provider — passing null must give a clear error.
        Action act = () => new ONETManager(storageProvider: null, oasisdna: null, networkType: P2PNetworkType.HoloNET);
        act.Should().Throw<InvalidOperationException>().WithMessage("*HoloOASIS*");
    }

    [Fact]
    public void Constructor_DnaTakesPriorityOverConstructorNetworkType()
    {
        // If DNA says "Internal", the manager should treat it as Internal even if P2PNetworkType.HoloNET
        // is passed as the constructor parameter — DNA is the authoritative source.
        var dna = BuildDna(networkType: "Internal");
        Action act = () => new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.HoloNET);
        act.Should().NotThrow("DNA override of Internal should prevent HoloNET from needing a provider");
    }

    [Fact]
    public async Task InitializeAsync_GeneratesNodeIdAndKeypair_WhenDnaNodeIdIsEmpty()
    {
        var dna = BuildDna(nodeId: "", nodePublicKey: "", nodePrivateKey: "");
        var manager = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);

        await manager.InitializeAsync();

        // DNA should now have a generated NodeId (64-char hex SHA-256) and non-empty keys.
        dna.OASIS.ONET.NodeId.Should().NotBeNullOrWhiteSpace()
            .And.HaveLength(64, "NodeId is SHA-256 hex of the public key");
        dna.OASIS.ONET.NodePublicKey.Should().NotBeNullOrWhiteSpace("public key should be base64-encoded");
        dna.OASIS.ONET.NodePrivateKey.Should().NotBeNullOrWhiteSpace("private key should be base64-encoded");
    }

    [Fact]
    public async Task InitializeAsync_PreservesExistingNodeId_WhenAlreadySet()
    {
        const string existingId = "aabbccddeeff00112233445566778899aabbccddeeff00112233445566778899";
        const string existingPub = "existingPublicKey==";
        const string existingPriv = "existingPrivateKey==";

        var dna = BuildDna(nodeId: existingId, nodePublicKey: existingPub, nodePrivateKey: existingPriv);
        var manager = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);

        await manager.InitializeAsync();

        dna.OASIS.ONET.NodeId.Should().Be(existingId, "existing NodeId must not be overwritten on re-init");
        dna.OASIS.ONET.NodePublicKey.Should().Be(existingPub);
        dna.OASIS.ONET.NodePrivateKey.Should().Be(existingPriv);
    }

    [Fact]
    public async Task InitializeAsync_SkipsBootstrapRegistration_WhenBootstrapServersEmpty()
    {
        // No servers configured — InitializeAsync should still complete without throwing.
        var dna = BuildDna(bootstrapServers: new List<string>());
        var manager = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);
        Func<Task> act = async () => await manager.InitializeAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InitializeAsync_WithUnreachableBootstrapServer_DoesNotThrow()
    {
        // Unreachable server should log a warning, not crash InitializeAsync.
        var dna = BuildDna(bootstrapServers: new List<string> { "http://127.0.0.1:19999" });
        var manager = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);
        Func<Task> act = async () => await manager.InitializeAsync();
        await act.Should().NotThrowAsync("bootstrap failures must be non-fatal");
    }

    [Fact]
    public void RegisterNodePublicKey_NodeIdDerivedFromKey_IsAccepted()
    {
        var manager = new ONETManager(storageProvider: null, oasisdna: null, networkType: P2PNetworkType.Internal);
        using var ecdsa = System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256);
        var publicKey = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
        var nodeId = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Convert.FromBase64String(publicKey))).ToLowerInvariant();

        manager.RegisterNodePublicKey(nodeId, publicKey).Should().BeTrue();
    }

    [Fact]
    public void RegisterNodePublicKey_NodeIdNotDerivedFromKey_IsRejected()
    {
        var manager = new ONETManager(storageProvider: null, oasisdna: null, networkType: P2PNetworkType.Internal);
        using var ecdsa = System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256);
        var publicKey = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());

        manager.RegisterNodePublicKey("victim-node-id", publicKey).Should().BeFalse();
    }

    [Fact]
    public void RegisterNodePublicKey_InvalidKey_IsRejected()
    {
        var manager = new ONETManager(storageProvider: null, oasisdna: null, networkType: P2PNetworkType.Internal);

        manager.RegisterNodePublicKey("nodeid123", Convert.ToBase64String(new byte[32])).Should().BeFalse();
    }

    [Fact]
    public async Task GetNetworkStatsAsync_ReturnsNetworkTypeAndNodeId()
    {
        var dna = BuildDna(networkType: "Internal", nodeId: "testnode");
        var manager = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);
        await manager.InitializeAsync();

        var result = await manager.GetNetworkStatsAsync();

        result.IsError.Should().BeFalse();
        result.Result.Should().ContainKey("networkType");
        result.Result["networkType"].Should().Be("Internal");
    }

    [Fact]
    public async Task StartNetworkAsync_ThenGetNetworkStatsAsync_HasUptime()
    {
        var manager = new ONETManager(storageProvider: null, oasisdna: null, networkType: P2PNetworkType.Internal);
        await manager.InitializeAsync();
        await manager.StartNetworkAsync();

        var stats = await manager.GetNetworkStatsAsync();

        stats.IsError.Should().BeFalse();
        stats.Result.Should().ContainKey("uptime");

        await manager.StopNetworkAsync();
    }

    [Fact]
    public async Task StopNetworkAsync_AfterStart_Succeeds()
    {
        var manager = new ONETManager(storageProvider: null, oasisdna: null, networkType: P2PNetworkType.Internal);
        await manager.InitializeAsync();
        await manager.StartNetworkAsync();

        var result = await manager.StopNetworkAsync();

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task GetConnectedNodesAsync_ReturnsNonNullList()
    {
        var manager = new ONETManager(storageProvider: null, oasisdna: null, networkType: P2PNetworkType.Internal);
        await manager.InitializeAsync();

        var result = await manager.GetConnectedNodesAsync();

        result.IsError.Should().BeFalse();
        result.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateONETConfigAsync_RejectsLocalNodeInPeerRegistrySetWithoutMutatingConfiguration()
    {
        var dna = BuildDna(nodeId: "local-node");
        dna.OASIS.ONET.CapabilityRegistryNodeIds = new List<string> { "existing-peer" };
        dna.OASIS.ONET.CapabilityRegistryQuorum = 1;
        var manager = new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);
        var update = BuildDna(nodeId: "ignored").OASIS.ONET;
        update.CapabilityRegistryNodeIds = new List<string> { "peer-a", "local-node" };
        update.CapabilityRegistryQuorum = 2;
        update.CapabilityRegistryReconciliationSeconds = 30;

        var result = await manager.UpdateONETConfigAsync(update);

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("own NodeId");
        dna.OASIS.ONET.CapabilityRegistryNodeIds.Should().Equal("existing-peer");
        dna.OASIS.ONET.CapabilityRegistryQuorum.Should().Be(1);
    }
}
