using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task StoredPropertyOutcomeUsesProtectedNativeIntentWithoutReplay(string providerName)
        => RunPropertyNativeFixture(providerName, true, true, true, true);

    private sealed class DeniedPropertyPreparationKeys : IHolonMetadataKeyResolver
    {
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = "STORED_PROPERTY_KEY_DENIED" };
    }

    private static async Task<int> VerifyStoredSelectedNativeStatus(IOASISStorageProvider provider,
        HolonManager manager, PropertyOutcomeBoundary probe, ProviderType authority, Holon source,
        Guid actor, HashSet<Guid> operations, Func<Guid, Task<string>> inspectIntent)
    {
        var policy = new HolonMetadataEncryptionPolicy("stored-property-recovery", "fixture-key-v1", new RecoveryPreparationKeys());
        var storage = (IConditionalHolonPropertyStorageProvider)provider;
        var preparationStorage = (IHolonWritePreparationStorageProvider)provider;
        var operation = Guid.NewGuid(); operations.Add(operation);
        var revision = CosmicConcurrency.GetProviderRevisionId(source, authority)!.Value;
        var submitted = StoredRecordJson.Project<Holon>(source);
        submitted.Name = "unselected local name"; submitted.Description = "protected selected description";
        var write = new ConditionalHolonWrite(operation, actor, authority, new[] { new HolonWriteCondition(source.Id, revision) });
        var selections = new Dictionary<Guid, IReadOnlyList<string>> { [source.Id] = new[] { "/Description" } };
        var original = new HolonConditionalPreparation(new[] { submitted }, write, false, false, 0,
            _ => policy, ConditionalHolonOperation.Save, selections).Seal();
        var baseline = StoredRecordJson.SerializeRuntime(source);
        var checks = 0;
        Type? Admit(string name, string assembly) => name == typeof(Holon).FullName
            && assembly == typeof(Holon).Assembly.GetName().Name ? typeof(Holon) : null;
        async Task<OASISResult<ConditionalHolonWriteOutcome>> Observe(
            Func<Task<OASISResult<ConditionalHolonWriteOutcome>>> read)
        {
            var before = await inspectIntent(operation);
            var result = await read();
            Assert.AreEqual(before, await inspectIntent(operation), "Status reads must leave native preparation and receipt bytes unchanged.");
            return result;
        }
        Task<OASISResult<ConditionalHolonWriteOutcome>> Recover()
            => manager.RecoverConditionalHolonPropertyWriteOutcomeAsync(operation, actor, authority, _ => policy, Admit);
        async Task<ConditionalHolonWriteOutcome> Status(ConditionalHolonReceiptState expected)
        {
            var beforeReads = probe.OutcomeReads; var beforePreparations = probe.PreparationReads;
            var result = await Observe(Recover);
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.IsTrue(result.IsLoaded);
            Assert.AreEqual(beforeReads + 1, probe.OutcomeReads);
            Assert.AreEqual(beforePreparations + 1, probe.PreparationReads);
            Assert.AreEqual(operation, result.Result.OperationId); Assert.AreEqual(actor, result.Result.ActorId);
            Assert.AreEqual(authority, result.Result.Authority); Assert.AreEqual(ConditionalHolonOperation.Save, result.Result.Operation);
            Assert.AreEqual(expected, result.Result.ReceiptState); Assert.IsNull(result.Result.ReceiptEvidence);
            return result.Result;
        }
        async Task<Holon> Read(int version)
        {
            var result = await provider.LoadHolonAsync(source.Id, false, false, 0, true, false, 0);
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(version, result.Result.Version);
            return StoredRecordJson.Project<Holon>(result.Result);
        }

        var reads = probe.OutcomeReads;
        var absent = await Observe(Recover);
        Assert.IsTrue(absent.IsError); Assert.AreEqual("COSMIC_PREPARATION_NOT_FOUND", absent.ErrorCode);
        Assert.IsNull(absent.Result); Assert.AreEqual(reads, probe.OutcomeReads);
        Assert.AreEqual(revision, CosmicConcurrency.GetProviderRevisionId(await Read(1), authority)); checks++;

        var claim = await preparationStorage.ClaimHolonWritePreparationAsync(original.Candidate);
        Assert.IsFalse(claim.IsError, claim.Message + claim.Exception);
        CollectionAssert.AreEqual(original.Candidate.Records.Select(row => row.Envelope).ToArray(),
            claim.Result.Records.Select(row => row.Envelope).ToArray());
        Assert.IsNull((await Status(ConditionalHolonReceiptState.Absent)).CommittedDeltaSummaryJson);
        Assert.AreEqual(revision, CosmicConcurrency.GetProviderRevisionId(await Read(1), authority)); checks++;

        selections[source.Id] = new[] { "/Name" };
        var changed = new HolonConditionalPreparation(new[] { submitted }, write, false, false, 0,
            _ => policy, ConditionalHolonOperation.Save, selections).Seal();
        var winner = await preparationStorage.ClaimHolonWritePreparationAsync(changed.Candidate);
        Assert.IsFalse(winner.IsError, winner.Message + winner.Exception);
        CollectionAssert.AreEqual(original.Candidate.Records.Select(row => row.Envelope).ToArray(),
            winner.Result.Records.Select(row => row.Envelope).ToArray());
        Assert.IsTrue(changed.RecoverProperty(winner.Result).IsError);
        var reconstructed = original.RecoverProperty(winner.Result);
        Assert.IsFalse(reconstructed.IsError, reconstructed.Message + reconstructed.Exception);
        Assert.AreEqual("/Description", reconstructed.Result.Selections[source.Id].Single()); checks++;

        var saved = await storage.ExecuteConditionalHolonPropertyWriteAsync(reconstructed.Result);
        Assert.IsFalse(saved.IsError, saved.Message + saved.Exception); Assert.IsTrue(saved.IsSaved);
        var current = await Read(2); var committedRevision = CosmicConcurrency.GetProviderRevisionId(current, authority);
        Assert.AreEqual("original", current.Name); Assert.AreEqual("protected selected description", current.Description);
        var summary = saved.MetaData[HolonCommittedDelta.ResultMetadataKey];
        Assert.AreEqual(summary, (await Status(ConditionalHolonReceiptState.Committed)).CommittedDeltaSummaryJson); checks++;

        submitted.Description = "caller changed after preparation"; submitted.Name = "later local name";
        var caller = StoredRecordJson.SerializeRuntime(submitted);
        Assert.AreEqual(summary, (await Status(ConditionalHolonReceiptState.Committed)).CommittedDeltaSummaryJson);
        Assert.AreEqual(committedRevision, CosmicConcurrency.GetProviderRevisionId(await Read(2), authority)); checks++;

        reads = probe.OutcomeReads;
        var wrongActor = await Observe(() => manager.RecoverConditionalHolonPropertyWriteOutcomeAsync(
            operation, Guid.NewGuid(), authority, _ => policy, Admit));
        Assert.IsTrue(wrongActor.IsError); Assert.IsNull(wrongActor.Result);
        var deniedPolicy = new HolonMetadataEncryptionPolicy("stored-property-recovery", "fixture-key-v1", new DeniedPropertyPreparationKeys());
        var deniedKey = await Observe(() => manager.RecoverConditionalHolonPropertyWriteOutcomeAsync(
            operation, actor, authority, _ => deniedPolicy, Admit));
        Assert.IsTrue(deniedKey.IsError); Assert.IsNull(deniedKey.Result);
        var deniedType = await Observe(() => manager.RecoverConditionalHolonPropertyWriteOutcomeAsync(
            operation, actor, authority, _ => policy, (_, _) => null!));
        Assert.IsTrue(deniedType.IsError); Assert.IsNull(deniedType.Result);
        Assert.AreEqual(reads, probe.OutcomeReads); checks++;

        var whole = await Observe(() => manager.RecoverConditionalHolonWriteOutcomeAsync(
            operation, actor, authority, _ => policy, Admit));
        Assert.IsTrue(whole.IsError); Assert.IsNull(whole.Result); Assert.AreEqual(reads, probe.OutcomeReads);
        var preparationReads = probe.PreparationReads;
        var empty = await Observe(() => manager.RecoverConditionalHolonPropertyWriteOutcomeAsync(
            operation, Guid.Empty, authority, _ => policy, Admit));
        Assert.IsTrue(empty.IsError); Assert.IsNull(empty.Result);
        Assert.AreEqual(preparationReads, probe.PreparationReads); Assert.AreEqual(reads, probe.OutcomeReads); checks++;

        Assert.AreEqual(committedRevision, CosmicConcurrency.GetProviderRevisionId(await Read(2), authority));
        Assert.AreEqual(baseline, StoredRecordJson.SerializeRuntime(source));
        Assert.AreEqual(caller, StoredRecordJson.SerializeRuntime(submitted)); checks++;
        Assert.AreEqual(8, checks);
        return checks;
    }
}
