using FluentAssertions;
using Moq;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RouteResponseOwnsInitialAndServingIdentityForAutomaticAndExplicitReads(bool synchronous)
    {
        var dna = CreateDna(HyperDriveModes.V2); dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var manager = new ProviderManager(null, dna) {
            IsAutoLoadBalanceEnabled = true, IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false };
        var first = CreateActiveProvider();
        var second = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "route-observation-sqlite");
        var id = Guid.NewGuid();
        foreach (var provider in new[] { first, second })
        {
            provider.Setup(value => value.LoadHolon(id, false, false, 0, true, false, 0))
                .Returns(() => new OASISResult<IHolon>(new Holon { Id = id }));
            provider.Setup(value => value.LoadHolonAsync(id, false, false, 0, true, false, 0))
                .ReturnsAsync(() => new OASISResult<IHolon>(new Holon { Id = id }));
            manager.RegisterProvider(provider.Object);
        }
        manager.SetAndReplaceAutoLoadBalanceListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS) });
        var router = new OASISHyperDrive(manager);
        async Task<OASISResult<IHolon>> Read(ProviderType preferred)
        {
            var request = new StorageOperationRequest { Operation = "LoadHolon", HolonId = id,
                PreferredProvider = preferred, LoadChildren = false, Recursive = false };
            return synchronous ? router.RouteRequest<IHolon>(request, LoadBalancingStrategy.RoundRobin)
                : await router.RouteRequestAsync<IHolon>(request, LoadBalancingStrategy.RoundRobin);
        }
        var automatic = await Read(ProviderType.Default);
        var explicitRead = await Read(ProviderType.SQLLiteDBOASIS);
        automatic.IsError.Should().BeFalse(automatic.Message);
        explicitRead.IsError.Should().BeFalse(explicitRead.Message);
        var observed = JObject.Parse(automatic.MetaData["hyperDriveRoute"]);
        observed["RequestedProvider"]!.Value<string>().Should().Be("Default");
        observed["InitialProvider"]!.Value<string>().Should().Be("MongoDBOASIS");
        observed["ServingProvider"]!.Value<string>().Should().Be("MongoDBOASIS");
        var other = JObject.Parse(explicitRead.MetaData["hyperDriveRoute"]);
        other["RequestedProvider"]!.Value<string>().Should().Be("SQLLiteDBOASIS");
        other["ServingProvider"]!.Value<string>().Should().Be("SQLLiteDBOASIS");
        // A later operation must not rewrite the earlier response's captured facts.
        JObject.Parse(automatic.MetaData["hyperDriveRoute"]).Should().BeEquivalentTo(observed);
    }

    [Fact]
    public async Task OverlappingReadsRetainTheirOwnSelectionWhileSharedDiagnosticChanges()
    {
        var dna = CreateDna(HyperDriveModes.V2); dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var manager = new ProviderManager(null, dna) {
            IsAutoLoadBalanceEnabled = true, IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false };
        var first = CreateActiveProvider();
        var second = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "overlapping-route-sqlite");
        var id = Guid.NewGuid();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<OASISResult<IHolon>>(TaskCreationOptions.RunContinuationsAsynchronously);
        first.Setup(value => value.LoadHolonAsync(id, false, false, 0, true, false, 0))
            .Callback(() => entered.TrySetResult()).Returns(release.Task);
        second.Setup(value => value.LoadHolonAsync(id, false, false, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new Holon { Id = id }));
        manager.RegisterProvider(first.Object); manager.RegisterProvider(second.Object);
        void Choose(ProviderType provider) => manager.SetAndReplaceAutoLoadBalanceListForProviders(new[] { new EnumValue<ProviderType>(provider) });
        StorageOperationRequest Request() => new() { Operation = "LoadHolon", HolonId = id, LoadChildren = false, Recursive = false };
        var router = new OASISHyperDrive(manager);
        Choose(ProviderType.MongoDBOASIS);
        var pending = router.RouteRequestAsync<IHolon>(Request(), LoadBalancingStrategy.RoundRobin);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            Choose(ProviderType.SQLLiteDBOASIS);
            var other = await router.RouteRequestAsync<IHolon>(Request(), LoadBalancingStrategy.RoundRobin);
            other.IsError.Should().BeFalse(other.Message);
            manager.LastProviderSelectionDiagnostic.SelectedProvider.Should().Be(ProviderType.SQLLiteDBOASIS);
            JObject.Parse(other.MetaData["hyperDriveRoute"])["ServingProvider"]!.Value<string>().Should().Be("SQLLiteDBOASIS");
        }
        finally { release.TrySetResult(new OASISResult<IHolon>(new Holon { Id = id })); }
        var original = await pending.WaitAsync(TimeSpan.FromSeconds(5));
        original.IsError.Should().BeFalse(original.Message);
        var observation = JObject.Parse(original.MetaData["hyperDriveRoute"]);
        observation["InitialProvider"]!.Value<string>().Should().Be("MongoDBOASIS");
        observation["ServingProvider"]!.Value<string>().Should().Be("MongoDBOASIS");
    }

    [Fact]
    public async Task FailedReadDoesNotClaimThatAProviderServedData()
    {
        var dna = CreateDna(HyperDriveModes.V2); dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var manager = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false };
        var provider = CreateActiveProvider(); var id = Guid.NewGuid();
        provider.Setup(value => value.LoadHolonAsync(id, false, false, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, ErrorCode = "native-failure" });
        manager.RegisterProvider(provider.Object);
        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IHolon>(new StorageOperationRequest {
            Operation = "LoadHolon", HolonId = id, PreferredProvider = ProviderType.MongoDBOASIS, LoadChildren = false, Recursive = false });
        result.IsError.Should().BeTrue();
        var observed = JObject.Parse(result.MetaData["hyperDriveRoute"]);
        observed["Succeeded"]!.Value<bool>().Should().BeFalse();
        observed["ServingProvider"]!.Type.Should().Be(JTokenType.Null);
    }
}
