using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class ProviderSelectionDeterminismTests
{
    [Fact]
    public void RoundRobinUsesStableProviderOrderingAndSequenceNotWallClock()
    {
        var manager = NewManager();

        var selected = Enumerable.Range(0, 6)
            .Select(_ => manager.SelectOptimalProviderForLoadBalancing(LoadBalancingStrategy.RoundRobin).Value)
            .ToArray();

        selected.Should().Equal(ProviderType.EthereumOASIS, ProviderType.IPFSOASIS, ProviderType.MongoDBOASIS,
            ProviderType.EthereumOASIS, ProviderType.IPFSOASIS, ProviderType.MongoDBOASIS);
    }

    [Fact]
    public void WeightedRoundRobinWithEqualUnknownMetricsIsStableAndFair()
    {
        var manager = NewManager();

        var selected = Enumerable.Range(0, 6)
            .Select(_ => manager.SelectOptimalProviderForLoadBalancing(LoadBalancingStrategy.WeightedRoundRobin).Value)
            .ToArray();

        selected.Should().Equal(ProviderType.EthereumOASIS, ProviderType.IPFSOASIS, ProviderType.MongoDBOASIS,
            ProviderType.EthereumOASIS, ProviderType.IPFSOASIS, ProviderType.MongoDBOASIS);
    }

    [Fact]
    public void AutomaticStrategyUsesTheManagersInjectedDnaConfiguration()
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS() };
        dna.OASIS.OASISHyperDriveConfig = new OASISHyperDriveConfig { DefaultStrategy = "RoundRobin" };
        var manager = new ProviderManager(null, dna) { IsAutoLoadBalanceEnabled = true };
        manager.AddProvidersToAutoLoadBalanceList(new[]
        {
            ProviderType.EthereumOASIS, ProviderType.IPFSOASIS, ProviderType.MongoDBOASIS
        });

        var selected = Enumerable.Range(0, 4)
            .Select(_ => manager.SelectOptimalProviderForLoadBalancing().Value)
            .ToArray();

        selected.Should().Equal(ProviderType.EthereumOASIS, ProviderType.IPFSOASIS,
            ProviderType.MongoDBOASIS, ProviderType.EthereumOASIS);
    }

    [Fact]
    public void PerformanceMetricsAreIsolatedPerProviderManagerRuntime()
    {
        var first = new ProviderManager(null);
        var second = new ProviderManager(null);

        first.PerformanceMonitor.RecordRequest(ProviderType.MongoDBOASIS, true, 12);

        first.PerformanceMonitor.GetMetrics(ProviderType.MongoDBOASIS).TotalRequests.Should().Be(1);
        second.PerformanceMonitor.GetMetrics(ProviderType.MongoDBOASIS).Should().BeNull();
        first.PerformanceMonitor.Should().NotBeSameAs(second.PerformanceMonitor);
    }

    [Fact]
    public void SelectionPublishesStructuredDiagnosticFromTheSameRuntimeInputs()
    {
        var manager = NewManager();
        manager.PerformanceMonitor.RecordRequest(ProviderType.MongoDBOASIS, true, 12, 0.004);

        var selected = manager.SelectOptimalProviderForLoadBalancing(LoadBalancingStrategy.Performance);
        var diagnostic = manager.LastProviderSelectionDiagnostic;

        diagnostic.Should().NotBeNull();
        diagnostic.RequestedStrategy.Should().Be(LoadBalancingStrategy.Performance);
        diagnostic.EffectiveStrategy.Should().Be(LoadBalancingStrategy.Performance);
        diagnostic.SelectedProvider.Should().Be(selected.Value);
        diagnostic.Reason.Should().Contain(selected.Value.ToString());
        diagnostic.Candidates.Select(x => x.Provider).Should().Equal(
            ProviderType.EthereumOASIS, ProviderType.IPFSOASIS, ProviderType.MongoDBOASIS);
        diagnostic.Candidates.Single(x => x.Provider == ProviderType.MongoDBOASIS)
            .ResponseTimeMs.Should().Be(12);
    }

    [Fact]
    public void DisabledLoadBalancingExplainsWhyCurrentProviderWasRetained()
    {
        var manager = new ProviderManager(null) { IsAutoLoadBalanceEnabled = false };

        var selected = manager.SelectOptimalProviderForLoadBalancing(LoadBalancingStrategy.CostBased);

        manager.LastProviderSelectionDiagnostic.SelectedProvider.Should().Be(selected.Value);
        manager.LastProviderSelectionDiagnostic.RequestedStrategy.Should().Be(LoadBalancingStrategy.CostBased);
        manager.LastProviderSelectionDiagnostic.Reason.Should().Contain("disabled");
        manager.LastProviderSelectionDiagnostic.Candidates.Should().BeEmpty();
    }

    private static ProviderManager NewManager()
    {
        var manager = new ProviderManager(null) { IsAutoLoadBalanceEnabled = true };
        manager.AddProvidersToAutoLoadBalanceList(new[]
        {
            ProviderType.EthereumOASIS, ProviderType.IPFSOASIS, ProviderType.MongoDBOASIS
        });
        return manager;
    }
}
