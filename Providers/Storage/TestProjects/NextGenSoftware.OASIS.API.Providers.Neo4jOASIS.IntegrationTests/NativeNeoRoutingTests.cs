using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;
using Neo = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class NativeHostedReplicaRestartTests
{
    private static async Task VerifyNativeReadRouting(ProviderManager registry, OASISDNA dna,
        Neo source, Holon input, Guid actor)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        OASISHyperDrive.DataDirectory = Path.Combine(root.FullName, "TestResults/Neo4jCurrent/routing-quota");
        Assert.IsFalse(registry.SetAndActivateCurrentStorageProvider(source).IsError);
        registry.IsAutoLoadBalanceEnabled = true;
        registry.AddProvidersToAutoLoadBalanceList(new[] { ProviderType.Neo4jOASIS, ProviderType.SQLLiteDBOASIS });

        // The native source and replica hold the same acknowledged record. Exercise
        // real reads while requiring admission to filter candidates before scoring.
        foreach (var plan in new[] { "Gold", "Free" })
        foreach (var strategy in new[] { LoadBalancingStrategy.RoundRobin,
            LoadBalancingStrategy.WeightedRoundRobin, LoadBalancingStrategy.LeastConnections,
            LoadBalancingStrategy.Geographic, LoadBalancingStrategy.CostBased,
            LoadBalancingStrategy.Performance, LoadBalancingStrategy.Auto })
        foreach (var mode in new[] { "sync", "async", "explicit" })
        {
            dna.OASIS.SubscriptionConfig.PlanType = plan;
            var router = new OASISHyperDrive(registry);
            var request = new StorageOperationRequest {
                Operation = "LoadHolon", HolonId = input.Id, AvatarId = actor
            };
            var result = mode == "sync" ? router.RouteRequest<IHolon>(request, strategy)
                : mode == "async" ? await router.RouteRequestAsync<IHolon>(request, strategy)
                : await router.LoadBalanceRequestAsync<IHolon>(request, strategy);
            var context = $"{plan}/{strategy}/{mode}: {result.Message}";
            Assert.IsFalse(result.IsError, context);
            Assert.AreEqual(input.Id, result.Result.Id, context);
            Assert.AreEqual(input.Name, result.Result.Name, context);
            Assert.AreEqual(actor, result.Result.CreatedByAvatarId, context);
            var diagnostic = registry.LastProviderSelectionDiagnostic;
            Assert.IsNotNull(diagnostic, context);
            var permitted = plan == "Free" ? new[] { ProviderType.SQLLiteDBOASIS }
                : new[] { ProviderType.Neo4jOASIS, ProviderType.SQLLiteDBOASIS };
            CollectionAssert.AreEquivalent(permitted, diagnostic.Candidates.Select(value => value.Provider).ToArray(), context);
            Assert.IsTrue(permitted.Contains(diagnostic.SelectedProvider), context);
            Assert.IsTrue(result.Result.ProviderUniqueStorageKey.ContainsKey(diagnostic.SelectedProvider), context);
        }
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        registry.IsAutoLoadBalanceEnabled = false;
        registry.IsAutoFailOverEnabled = true;
        Assert.IsFalse(registry.SetAndReplaceAutoFailOverListForProviders(new[] {
            new EnumValue<ProviderType>(ProviderType.Neo4jOASIS),
            new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError);

        // This is the shipped sandbox's avatar-scoped refusal, not a stopped Neo4j
        // process. The owner fails over to real SQLite; a peer still reads real Neo4j.
        ProviderFaultGate.Enable();
        ProviderFaultGate.Fault(actor, ProviderType.Neo4jOASIS, TimeSpan.FromMinutes(1));
        var priorAvatar = OASISRequestContext.CurrentAvatarId;
        try
        {
            OASISRequestContext.CurrentAvatarId = actor;
            foreach (var mode in new[] { "sync", "async", "explicit" })
            {
                var router = new OASISHyperDrive(registry);
                var request = new StorageOperationRequest {
                    Operation = "LoadHolon", HolonId = input.Id, AvatarId = actor,
                    PreferredProvider = ProviderType.Neo4jOASIS
                };
                var result = mode == "sync" ? router.RouteRequest<IHolon>(request)
                    : mode == "async" ? await router.RouteRequestAsync<IHolon>(request)
                    : await router.FailoverRequestAsync<IHolon>(request);
                Assert.IsFalse(result.IsError, result.Message);
                Assert.AreEqual(input.Id, result.Result.Id);
                Assert.AreEqual(actor, result.Result.CreatedByAvatarId);
                var diagnostic = router.LastFailoverDiagnostic;
                Assert.IsNotNull(diagnostic);
                Assert.IsTrue(diagnostic.Succeeded);
                Assert.AreEqual(ProviderType.SQLLiteDBOASIS, diagnostic.SelectedProvider);
                Assert.AreEqual(2, diagnostic.Attempts.Count);
                Assert.AreEqual("HYPERDRIVE_PROVIDER_OFFLINE", diagnostic.Attempts[0].ErrorCode);
                Assert.AreEqual(ProviderType.Neo4jOASIS, diagnostic.Attempts[0].Provider);
                Assert.AreEqual(ProviderType.SQLLiteDBOASIS, diagnostic.Attempts[1].Provider);
            }
            var peer = Guid.NewGuid();
            OASISRequestContext.CurrentAvatarId = peer;
            var peerRouter = new OASISHyperDrive(registry);
            var peerRead = await peerRouter.RouteRequestAsync<IHolon>(new StorageOperationRequest {
                Operation = "LoadHolon", HolonId = input.Id, AvatarId = peer,
                PreferredProvider = ProviderType.Neo4jOASIS
            });
            Assert.IsFalse(peerRead.IsError, peerRead.Message);
            Assert.AreEqual(input.Id, peerRead.Result.Id);
            Assert.IsNull(peerRouter.LastFailoverDiagnostic);
            Assert.AreEqual(3L, ProviderFaultGate.GetRefusals(actor)[ProviderType.Neo4jOASIS]);
            Assert.AreEqual(0, ProviderFaultGate.GetFaulted(peer).Count);
        }
        finally
        {
            ProviderFaultGate.Restore(actor, ProviderType.Neo4jOASIS);
            OASISRequestContext.CurrentAvatarId = priorAvatar;
        }
        CollectionAssert.AreEquivalent(new[] { ProviderType.Neo4jOASIS, ProviderType.SQLLiteDBOASIS },
            registry.GetProviderAutoLoadBalanceList().Select(value => value.Value).ToArray());
    }
}
