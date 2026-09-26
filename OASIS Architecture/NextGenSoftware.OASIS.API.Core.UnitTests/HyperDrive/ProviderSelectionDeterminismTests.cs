using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
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
