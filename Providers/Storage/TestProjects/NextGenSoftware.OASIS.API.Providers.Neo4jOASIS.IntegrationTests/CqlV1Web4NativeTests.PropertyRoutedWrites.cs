using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using Router = NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task PreparedSelectedWritesUseNativeHyperDriveDispatchWithoutDowngrade(string providerName)
        => RunPropertyNativeFixture(providerName, true, false, true, false, true);

    private static async Task<int> VerifyRoutedSelectedNativeWrites(IOASISStorageProvider provider,
        Router router, PropertyOutcomeBoundary probe, ProviderType authority, Holon source,
        Guid actor, HashSet<Guid> operations)
    {
        var policy = new HolonMetadataEncryptionPolicy("routed-property-write", "fixture-key-v1", new RecoveryPreparationKeys());
        var checks = 0;
        var baseline = StoredRecordJson.SerializeRuntime(source);
        var originalComparison = HolonChangeTracking.GetPropertyValues(source, "/Description").Result.OriginalJson;
        Guid Revision(IHolon value) => CosmicConcurrency.GetProviderRevisionId(value, authority)!.Value;
        StorageOperationRequest Request(Holon proposed, Guid revision, IReadOnlyList<string> paths, bool single = false)
        {
            var operation = Guid.NewGuid(); operations.Add(operation);
            var write = new ConditionalHolonWrite(operation, actor, authority, new[] { new HolonWriteCondition(source.Id, revision) });
            var selections = new Dictionary<Guid, IReadOnlyList<string>> { [source.Id] = paths };
            var preparation = new HolonConditionalPreparation(new[] { proposed }, write, false, false, 0,
                _ => policy, ConditionalHolonOperation.Save, selections).Seal();
            return new StorageOperationRequest
            {
                Operation = single ? "SaveHolon" : "SaveHolons", Payload = single ? (object)proposed : new[] { proposed },
                AvatarId = actor, PreferredProvider = authority, RequirePreferredProvider = true, AutoReplicate = false,
                SaveChildren = false, Recursive = false, MaxChildDepth = 0, ConditionalWrite = write,
                ConditionalPreparation = preparation, ConditionalPropertySelections = selections
            };
        }
        async Task<Holon> Read(int version)
        {
            var result = await provider.LoadHolonAsync(source.Id, false, false, 0, true, false, 0);
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(version, result.Result.Version);
            return StoredRecordJson.Project<Holon>(result.Result);
        }

        var proposed = StoredRecordJson.Project<Holon>(source);
        proposed.Name = "unselected async name"; proposed.Description = "routed selected description";
        var proposedBefore = StoredRecordJson.SerializeRuntime(proposed);
        var paths = new[] { "/Description" };
        var first = Request(proposed, Revision(source), paths);
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var beforeResource = probe.BeforeResource;
        probe.BeforeResource = async (operation, ids) =>
        {
            await beforeResource(operation, ids);
            entered.TrySetResult(true);
            await release.Task;
        };
        var pending = router.RouteRequestAsync<IEnumerable<IHolon>>(first);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(20));
            paths[0] = "/Name";
        }
        finally
        {
            release.TrySetResult(true); probe.BeforeResource = beforeResource;
        }
        var saved = await pending;
        Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
        Assert.AreEqual(2, saved.Result.Single().Version);
        Assert.AreEqual("/Description", first.ConditionalPropertySelections[source.Id].Single()); checks++;
        var current = await Read(2);
        Assert.AreEqual("original", current.Name); Assert.AreEqual("routed selected description", current.Description);
        Assert.AreEqual(proposedBefore, StoredRecordJson.SerializeRuntime(proposed)); checks++;
        var firstSummary = saved.MetaData[HolonCommittedDelta.ResultMetadataKey];

        var secondProposal = StoredRecordJson.Project<Holon>(current);
        secondProposal.Name = "unselected sync name";
        secondProposal.MetaData["unselected-extra"] = 999L;
        secondProposal.MetaData.Remove("nullable");
        var secondBefore = StoredRecordJson.SerializeRuntime(secondProposal);
        var second = Request(secondProposal, Revision(current), new[] { "/MetaData/nullable" }, true);
        var single = router.RouteRequest<IHolon>(second);
        Assert.IsFalse(single.IsError, single.Message + single.Exception);
        Assert.AreEqual(3, single.Result.Version);
        current = await Read(3); var currentRevision = Revision(current);
        Assert.AreEqual("original", current.Name); Assert.IsFalse(current.MetaData.ContainsKey("nullable"));
        Assert.IsFalse(current.MetaData.ContainsKey("unselected-extra"));
        Assert.AreEqual(secondBefore, StoredRecordJson.SerializeRuntime(secondProposal)); checks++;

        var replayed = await router.RouteRequestAsync<IEnumerable<IHolon>>(first);
        Assert.IsFalse(replayed.IsError, replayed.Message + replayed.Exception);
        Assert.AreEqual(2, replayed.Result.Single().Version);
        Assert.AreEqual(firstSummary, replayed.MetaData[HolonCommittedDelta.ResultMetadataKey]);
        Assert.AreEqual(currentRevision, Revision(await Read(3))); checks++;

        var plainProposal = StoredRecordJson.Project<Holon>(current);
        plainProposal.Name = "unselected name without preparation";
        plainProposal.Description = "selected without protected preparation";
        var plain = Request(plainProposal, currentRevision, new[] { "/Description" });
        plain.ConditionalPreparation = null;
        var claimsBeforePlain = probe.PreparationIntents.Count;
        var plainResult = await router.RouteRequestAsync<IEnumerable<IHolon>>(plain);
        Assert.IsFalse(plainResult.IsError, plainResult.Message + plainResult.Exception);
        current = await Read(4); currentRevision = Revision(current);
        Assert.AreEqual("original", current.Name);
        Assert.AreEqual("selected without protected preparation", current.Description);
        Assert.AreEqual(claimsBeforePlain, probe.PreparationIntents.Count); checks++;

        var preparationCalls = probe.PreparationIntents.Count;
        var selectedCalls = probe.PropertySyncWrites + probe.PropertyAsyncWrites;
        foreach (var mode in new[] { "mask", "actor", "authority", "unpinned", "roots" })
        {
            var denied = Request(StoredRecordJson.Project<Holon>(current), currentRevision, new[] { "/Description" });
            if (mode == "mask") denied.ConditionalPropertySelections = new Dictionary<Guid, IReadOnlyList<string>> { [source.Id] = new[] { "/Name" } };
            if (mode == "actor") denied.AvatarId = Guid.NewGuid();
            if (mode == "authority") denied.PreferredProvider = authority == ProviderType.MongoDBOASIS ? ProviderType.SQLLiteDBOASIS : ProviderType.MongoDBOASIS;
            if (mode == "unpinned") denied.RequirePreferredProvider = false;
            if (mode == "roots") denied.Payload = new[] { new Holon { Id = Guid.NewGuid() } };
            var result = await router.RouteRequestAsync<IEnumerable<IHolon>>(denied);
            Assert.IsTrue(result.IsError); Assert.AreEqual("COSMIC_PROPERTY_WRITE_INTENT_INVALID", result.ErrorCode);
            Assert.IsNull(result.Result);
        }
        Assert.AreEqual(preparationCalls, probe.PreparationIntents.Count);
        Assert.AreEqual(selectedCalls, probe.PropertySyncWrites + probe.PropertyAsyncWrites);
        Assert.AreEqual(currentRevision, Revision(await Read(4))); checks++;

        var outcome = await ((IConditionalHolonPropertyStorageProvider)provider).ReadConditionalHolonPropertyWriteOutcomeAsync(
            first.ConditionalPreparation.RecoverProperty(first.ConditionalPreparation.Candidate).Result);
        Assert.IsFalse(outcome.IsError, outcome.Message + outcome.Exception);
        Assert.AreEqual(ConditionalHolonReceiptState.Committed, outcome.Result.ReceiptState);
        Assert.AreEqual(firstSummary, outcome.Result.CommittedDeltaSummaryJson); checks++;

        Assert.AreEqual(1, probe.PropertySyncWrites); Assert.AreEqual(3, probe.PropertyAsyncWrites);
        Assert.AreEqual(1, probe.MutationIntents.Count, "Only initial create may use whole-record dispatch."); checks++;
        Assert.AreEqual(baseline, StoredRecordJson.SerializeRuntime(source));
        Assert.AreEqual(originalComparison, HolonChangeTracking.GetPropertyValues(source, "/Description").Result.OriginalJson);
        Assert.IsFalse(HolonChangeTracking.HasChanges(source)); checks++;
        Assert.AreEqual(9, checks);
        return checks;
    }
}
