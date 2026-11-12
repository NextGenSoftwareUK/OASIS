using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.Providers.TestSupport;

internal static class NativePriorityRouting
{
    public static async Task VerifyAsync(IOASISStorageProvider first, IOASISStorageProvider second,
        IOASISStorageProvider unavailableFirst, IOASISStorageProvider unavailableSecond, bool sync)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var providerTypes = new[] { first.ProviderType.Value, second.ProviderType.Value };
        var candidates = providerTypes.Select(type => new EnumValue<ProviderType>(type)).ToArray();
        var replication = new ProviderManager(null, dna) { IsAutoReplicationEnabled = true };
        replication.RegisterProvider(first);
        replication.RegisterProvider(second);
        Assert.IsFalse(replication.SetAndReplaceAutoReplicationListForProviders(candidates).IsError);
        var replicationRouter = new OASISHyperDrive(replication);
        var records = new List<Holon>();
        foreach (var primary in providerTypes)
        {
            var holon = new Holon { Id = Guid.NewGuid(), Name = $"priority routing {primary}", CreatedByAvatarId = Guid.NewGuid() };
            var request = new StorageOperationRequest { Operation = "SaveHolon", PreferredProvider = primary, Payload = holon, SaveChildren = false };
            var saved = sync ? replicationRouter.RouteRequest<IHolon>(request) : await replicationRouter.RouteRequestAsync<IHolon>(request);
            Assert.IsFalse(saved.IsError || saved.IsWarning, saved.Message);
            foreach (var provider in new IOASISStorageProvider[] { first, second })
            {
                var direct = await provider.LoadHolonAsync(holon.Id, loadChildren: false);
                Assert.IsFalse(direct.IsError, direct.Message);
                AssertRecord(holon, direct.Result);
            }
            records.Add(holon);
        }

        var balancing = new ProviderManager(null, dna) { IsAutoLoadBalanceEnabled = true };
        balancing.RegisterProvider(first);
        balancing.RegisterProvider(second);
        Assert.IsFalse(balancing.SetAndReplaceAutoLoadBalanceListForProviders(candidates).IsError);
        var balancedRouter = new OASISHyperDrive(balancing);
        var selected = records[0];
        var served = new List<string>();
        for (var index = 0; index < 6; index++)
        {
            var read = new StorageOperationRequest { Operation = "LoadHolon", HolonId = selected.Id, LoadChildren = false };
            var result = sync ? balancedRouter.RouteRequest<IHolon>(read, LoadBalancingStrategy.RoundRobin)
                : await balancedRouter.RouteRequestAsync<IHolon>(read, LoadBalancingStrategy.RoundRobin);
            Assert.IsFalse(result.IsError, result.Message);
            AssertRecord(selected, result.Result);
            using var route = JsonDocument.Parse(result.MetaData["hyperDriveRoute"]);
            var initial = route.RootElement.GetProperty("InitialProvider").GetString();
            Assert.AreEqual(initial, route.RootElement.GetProperty("ServingProvider").GetString());
            Assert.AreEqual("RoundRobin", route.RootElement.GetProperty("RequestedStrategy").GetString());
            served.Add(initial!);
        }
        foreach (var provider in providerTypes)
            Assert.AreEqual(3, served.Count(value => value == provider.ToString()));

        await VerifyFailoverAsync(dna, candidates, selected, sync, unavailableFirst, second);
        await VerifyFailoverAsync(dna, candidates, selected, sync, unavailableSecond, first);
    }

    private static async Task VerifyFailoverAsync(OASISDNA dna, EnumValue<ProviderType>[] candidates,
        Holon expected, bool sync, IOASISStorageProvider unavailable, IOASISStorageProvider replica)
    {
        var failover = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false };
        failover.RegisterProvider(unavailable);
        failover.RegisterProvider(replica);
        Assert.IsFalse(failover.SetAndReplaceAutoFailOverListForProviders(candidates).IsError);
        var router = new OASISHyperDrive(failover);
        var source = unavailable.ProviderType.Value;
        StorageOperationRequest ReadSource() => new() { Operation = "LoadHolon", HolonId = expected.Id,
            LoadChildren = false, PreferredProvider = source };

        var rejected = sync ? router.RouteRequest<IHolon>(ReadSource()) : await router.RouteRequestAsync<IHolon>(ReadSource());
        Assert.IsTrue(rejected.IsError, rejected.Message);
        failover.IsAutoFailOverEnabled = true;
        var recovered = sync ? router.RouteRequest<IHolon>(ReadSource()) : await router.RouteRequestAsync<IHolon>(ReadSource());
        Assert.IsFalse(recovered.IsError, recovered.Message);
        AssertRecord(expected, recovered.Result);
        using var route = JsonDocument.Parse(recovered.MetaData["hyperDriveRoute"]);
        Assert.AreEqual(source.ToString(), route.RootElement.GetProperty("RequestedProvider").GetString());
        Assert.AreEqual(replica.ProviderType.Value.ToString(), route.RootElement.GetProperty("ServingProvider").GetString());
    }

    private static void AssertRecord(Holon expected, IHolon? actual)
    {
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.Name, actual.Name);
        Assert.AreEqual(expected.CreatedByAvatarId, actual.CreatedByAvatarId);
    }
}
