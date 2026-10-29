using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.Utilities;
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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentAvatarRoutingAndFaultsRemainIsolatedThroughActualV2Execution(bool synchronous)
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var manager = new ProviderManager(null, dna) {
            IsAutoFailOverEnabled = true, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = true };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "scoped-primary");
        var secondary = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "scoped-secondary");
        var id = Guid.NewGuid();
        foreach (var provider in new[] { primary, secondary })
        {
            var stored = new Holon { Id = id, Name = provider == primary ? "primary" : "secondary" };
            provider.Setup(value => value.LoadHolon(id, true, true, 0, true, false, 0)).Returns(new OASISResult<IHolon>(stored));
            provider.Setup(value => value.LoadHolonAsync(id, true, true, 0, true, false, 0)).ReturnsAsync(new OASISResult<IHolon>(stored));
            provider.Setup(value => value.ActivateProvider()).Returns(new OASISResult<bool>(true));
            provider.Setup(value => value.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
            manager.RegisterProvider(provider.Object).Should().BeTrue();
        }
        manager.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        manager.AddProvidersToAutoLoadBalanceList(new[] { ProviderType.MongoDBOASIS });
        manager.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS) }).IsError.Should().BeFalse();
        var visitor = Guid.NewGuid();
        var other = Guid.NewGuid();
        var parentAvatar = OASISRequestContext.CurrentAvatarId;
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrivals = 0;
        Task<(string Name, bool Failover, HyperDriveFailoverDiagnostic Diagnostic)> Read(Guid actor, bool faulted) => Task.Run(async () =>
        {
            OASISRequestContext.CurrentAvatarId = actor;
            OASISRequestContext.AvatarRoutingOverrides = new ProviderRoutingOverrides(autoFailoverEnabled: faulted,
                loadBalancingProviders: new[] { faulted ? ProviderType.SQLLiteDBOASIS : ProviderType.MongoDBOASIS },
                failoverProviders: new[] { ProviderType.SQLLiteDBOASIS });
            OASISRequestContext.RequestRoutingOverrides = faulted
                ? new ProviderRoutingOverrides(loadBalancingProviders: new[] { ProviderType.MongoDBOASIS }) : null;
            if (Interlocked.Increment(ref arrivals) == 2) ready.TrySetResult();
            await gate.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var router = new OASISHyperDrive(manager);
            var request = new StorageOperationRequest { Operation = "LoadHolon", HolonId = id };
            var result = synchronous ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);
            result.IsError.Should().BeFalse(result.Message + result.Exception);
            return (result.Result.Name, manager.IsAutoFailOverEnabled, router.LastFailoverDiagnostic);
        });
        ProviderFaultGate.Enable();
        ProviderFaultGate.Fault(visitor, ProviderType.MongoDBOASIS, TimeSpan.FromMinutes(1));
        try
        {
            var a = Read(visitor, true);
            var b = Read(other, false);
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
            gate.SetResult();
            var results = await Task.WhenAll(a, b);
            results[0].Name.Should().Be("secondary");
            results[0].Failover.Should().BeTrue();
            results[0].Diagnostic.Should().NotBeNull("the request override must first select the faulted Mongo provider");
            results[0].Diagnostic.SelectedProvider.Should().Be(ProviderType.SQLLiteDBOASIS);
            results[0].Diagnostic.Attempts[0].ErrorCode.Should().Be("HYPERDRIVE_PROVIDER_OFFLINE");
            results[1].Name.Should().Be("primary");
            results[1].Failover.Should().BeFalse();
            results[1].Diagnostic.Should().BeNull();
            ProviderFaultGate.GetRefusals(visitor)[ProviderType.MongoDBOASIS].Should().Be(1);
            ProviderFaultGate.GetFaulted(other).Should().BeEmpty();
            OASISRequestContext.CurrentAvatarId.Should().Be(parentAvatar);
            manager.IsAutoFailOverEnabled.Should().BeTrue();
            manager.GetProviderAutoLoadBalanceList().Select(value => value.Value).Should().Equal(ProviderType.MongoDBOASIS);
            manager.GetProviderAutoFailOverList().Select(value => value.Value).Should().Equal(ProviderType.MongoDBOASIS);
            foreach (var provider in new[] { primary, secondary })
                provider.Invocations.Count(call => call.Method.Name is "LoadHolon" or "LoadHolonAsync").Should().Be(1,
                    "the faulted avatar must be refused before touching primary storage");
        }
        finally { ProviderFaultGate.Restore(visitor, ProviderType.MongoDBOASIS); }
    }

}
