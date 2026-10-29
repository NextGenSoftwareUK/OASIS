using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CurrentProviderFallbackDoesNotRewriteTheNodeLoadBalancingConfiguration(bool synchronous)
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var manager = new ProviderManager(null, dna) {
            IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = true };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "routing-isolation-primary");
        var id = Guid.NewGuid();
        var stored = new Holon { Id = id, Name = "owned-routing-record" };
        primary.Setup(value => value.LoadHolon(id, true, true, 0, true, false, 0)).Returns(new OASISResult<IHolon>(stored));
        primary.Setup(value => value.LoadHolonAsync(id, true, true, 0, true, false, 0)).ReturnsAsync(new OASISResult<IHolon>(stored));
        primary.Setup(value => value.ActivateProvider()).Returns(new OASISResult<bool>(true));
        primary.Setup(value => value.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        manager.RegisterProvider(primary.Object).Should().BeTrue();
        manager.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        manager.GetProviderAutoLoadBalanceList().Should().BeEmpty();
        var router = new OASISHyperDrive(manager);
        var request = new StorageOperationRequest { Operation = "LoadHolon", HolonId = id };

        var result = synchronous ? router.RouteRequest<IHolon>(request)
            : await router.RouteRequestAsync<IHolon>(request);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Id.Should().Be(id);
        manager.GetProviderAutoLoadBalanceList().Should().BeEmpty("candidate fallback belongs only to this request");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmptyConfiguredListUsesTheCurrentProviderForEachNewRequest(bool synchronous)
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var manager = new ProviderManager(null, dna) {
            IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = true };
        var id = Guid.NewGuid();
        var first = CreateActiveProvider(ProviderType.MongoDBOASIS, "first-current");
        var second = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "second-current");
        foreach (var provider in new[] { first, second })
        {
            var stored = new Holon { Id = id, Name = provider == first ? "first" : "second" };
            provider.Setup(value => value.LoadHolon(id, true, true, 0, true, false, 0)).Returns(new OASISResult<IHolon>(stored));
            provider.Setup(value => value.LoadHolonAsync(id, true, true, 0, true, false, 0)).ReturnsAsync(new OASISResult<IHolon>(stored));
            provider.Setup(value => value.ActivateProvider()).Returns(new OASISResult<bool>(true));
            provider.Setup(value => value.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
            manager.RegisterProvider(provider.Object).Should().BeTrue();
        }
        var router = new OASISHyperDrive(manager);
        async Task<string> Read()
        {
            var request = new StorageOperationRequest { Operation = "LoadHolon", HolonId = id };
            var result = synchronous ? router.RouteRequest<IHolon>(request)
                : await router.RouteRequestAsync<IHolon>(request);
            result.IsError.Should().BeFalse(result.Message);
            return result.Result.Name;
        }
        manager.SetAndActivateCurrentStorageProvider(first.Object).IsError.Should().BeFalse();
        (await Read()).Should().Be("first");
        manager.SetAndActivateCurrentStorageProvider(second.Object).IsError.Should().BeFalse();
        (await Read()).Should().Be("second", "an earlier request cannot leave a hidden persistent candidate");
    }
}
