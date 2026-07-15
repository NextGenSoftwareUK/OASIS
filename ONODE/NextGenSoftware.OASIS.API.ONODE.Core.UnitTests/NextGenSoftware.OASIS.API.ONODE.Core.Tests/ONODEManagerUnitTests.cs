using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests;

public class ONODEManagerUnitTests
{
    private static OASISDNA BuildDna(string nodeId = "testnode")
    {
        var dna = new OASISDNA();
        dna.OASIS.ONET = new ONETConfig { NodeId = nodeId, NetworkType = "Internal" };
        return dna;
    }

    private static ONETManager BuildOnetManager(OASISDNA? dna = null)
        => new ONETManager(storageProvider: null, oasisdna: dna, networkType: P2PNetworkType.Internal);

    [Fact]
    public void Constructor_WithoutOnetManager_DoesNotThrow()
    {
        Action act = () => new ONODEManager(storageProvider: null);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithOnetManager_DoesNotThrow()
    {
        var onet = BuildOnetManager();
        Action act = () => new ONODEManager(storageProvider: null, oasisdna: null, onetManager: onet);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task GetOASISDNAAsync_ReturnsDnaPassedToConstructor()
    {
        var dna = BuildDna();
        var manager = new ONODEManager(storageProvider: null, oasisdna: dna);

        var result = await manager.GetOASISDNAAsync();

        result.IsError.Should().BeFalse();
        result.Result.Should().BeSameAs(dna);
    }

    [Fact]
    public async Task UpdateOASISDNAAsync_ReplacesStoredDna()
    {
        var dna1 = BuildDna("node1");
        var dna2 = BuildDna("node2");
        var manager = new ONODEManager(storageProvider: null, oasisdna: dna1);

        await manager.UpdateOASISDNAAsync(dna2);
        var result = await manager.GetOASISDNAAsync();

        result.Result.Should().BeSameAs(dna2);
    }

    [Fact]
    public async Task GetNodeStatusAsync_InitialState_IsNotRunning()
    {
        var manager = new ONODEManager(storageProvider: null);

        var result = await manager.GetNodeStatusAsync();

        result.IsError.Should().BeFalse();
        result.Result!.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task StartNodeAsync_SetsNodeRunning()
    {
        var manager = new ONODEManager(storageProvider: null);

        var startResult = await manager.StartNodeAsync();
        var statusResult = await manager.GetNodeStatusAsync();

        startResult.IsError.Should().BeFalse();
        statusResult.Result!.IsRunning.Should().BeTrue();
    }

    [Fact]
    public async Task StartNodeAsync_WhenAlreadyRunning_ReturnsError()
    {
        var manager = new ONODEManager(storageProvider: null);
        await manager.StartNodeAsync();

        var result = await manager.StartNodeAsync();

        result.IsError.Should().BeTrue("cannot start an already-running node");
    }

    [Fact]
    public async Task StopNodeAsync_WhenRunning_StopsNode()
    {
        var manager = new ONODEManager(storageProvider: null);
        await manager.StartNodeAsync();

        var stopResult = await manager.StopNodeAsync();
        var statusResult = await manager.GetNodeStatusAsync();

        stopResult.IsError.Should().BeFalse();
        statusResult.Result!.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task StopNodeAsync_WhenNotRunning_ReturnsError()
    {
        var manager = new ONODEManager(storageProvider: null);

        var result = await manager.StopNodeAsync();

        result.IsError.Should().BeTrue("cannot stop a node that was never started");
    }

    [Fact]
    public async Task RestartNodeAsync_LeavesNodeRunning()
    {
        var manager = new ONODEManager(storageProvider: null);
        await manager.StartNodeAsync();

        var result = await manager.RestartNodeAsync();
        var status = await manager.GetNodeStatusAsync();

        result.IsError.Should().BeFalse();
        status.Result!.IsRunning.Should().BeTrue();
    }

    [Fact]
    public async Task GetNodeInfoAsync_ReturnsNodeIdFromDna()
    {
        var dna = BuildDna(nodeId: "mynodeid");
        var manager = new ONODEManager(storageProvider: null, oasisdna: dna);

        var result = await manager.GetNodeInfoAsync();

        result.IsError.Should().BeFalse();
        result.Result!.NodeId.Should().Be("mynodeid");
    }

    [Fact]
    public async Task GetNodeMetricsAsync_ReturnsNonZeroMemory()
    {
        var manager = new ONODEManager(storageProvider: null);

        var result = await manager.GetNodeMetricsAsync();

        result.IsError.Should().BeFalse();
        result.Result!.MemoryUsage.Should().BeGreaterThan(0, "process working set is always > 0");
    }

    [Fact]
    public async Task GetNodeLogsAsync_ContainsInitializationEntry()
    {
        var manager = new ONODEManager(storageProvider: null);

        var result = await manager.GetNodeLogsAsync();

        result.IsError.Should().BeFalse();
        result.Result.Should().Contain(e => e.Contains("initialized"), "constructor logs an init entry");
    }

    [Fact]
    public async Task StartNodeAsync_AppendsLogEntry()
    {
        var manager = new ONODEManager(storageProvider: null);
        await manager.StartNodeAsync();

        var logs = await manager.GetNodeLogsAsync();

        logs.Result.Should().Contain(e => e.Contains("started"));
    }

    [Fact]
    public async Task UpdateNodeConfigAsync_WithNullConfig_ReturnsError()
    {
        var manager = new ONODEManager(storageProvider: null);

        var result = await manager.UpdateNodeConfigAsync(null!);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateNodeConfigAsync_StoresAndRetrieves()
    {
        var manager = new ONODEManager(storageProvider: null);
        var config = new Dictionary<string, object> { ["key1"] = "value1", ["key2"] = 42 };

        await manager.UpdateNodeConfigAsync(config);
        var result = await manager.GetNodeConfigAsync();

        result.IsError.Should().BeFalse();
        result.Result.Should().ContainKey("key1");
        result.Result["key1"].Should().Be("value1");
    }

    [Fact]
    public async Task GetConnectedPeersAsync_WithoutOnetManager_ReturnsEmptyList()
    {
        // When no ONETManager is injected, peers list should be empty (not an error).
        var manager = new ONODEManager(storageProvider: null);

        var result = await manager.GetConnectedPeersAsync();

        result.IsError.Should().BeFalse();
        result.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNodeStatsAsync_ReturnsNodeRunningKey()
    {
        var manager = new ONODEManager(storageProvider: null);

        var result = await manager.GetNodeStatsAsync();

        result.IsError.Should().BeFalse();
        result.Result.Should().ContainKey("nodeRunning");
    }

    [Fact]
    public async Task GetNodeStatsAsync_WithInjectedOnetManager_ContainsOnetPrefixedKeys()
    {
        var onet = BuildOnetManager();
        await onet.InitializeAsync();
        var manager = new ONODEManager(storageProvider: null, onetManager: onet);

        var result = await manager.GetNodeStatsAsync();

        result.IsError.Should().BeFalse();
        result.Result.Keys.Should().Contain(k => k.StartsWith("onet_"), "ONET stats are merged with onet_ prefix");
    }

    [Fact]
    public async Task StartNodeAsync_WithInjectedOnetManager_StartsOnetNetwork()
    {
        var onet = BuildOnetManager();
        await onet.InitializeAsync();
        var manager = new ONODEManager(storageProvider: null, onetManager: onet);

        var result = await manager.StartNodeAsync();

        result.IsError.Should().BeFalse();

        // Verify ONET actually started by checking stats contain uptime key.
        var stats = await onet.GetNetworkStatsAsync();
        stats.Result.Should().ContainKey("uptime");

        await manager.StopNodeAsync();
    }
}
